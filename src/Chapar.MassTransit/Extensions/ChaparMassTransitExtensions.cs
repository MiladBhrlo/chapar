using System.Reflection;
using Chapar.Core.Abstractions;
using Chapar.Core.Attributes;
using Chapar.Core.Inbox;
using Chapar.Core.Metrics;
using Chapar.Core.Outbox;
using Chapar.MassTransit.Adapters;
using Chapar.MassTransit.Bus;
using Chapar.MassTransit.Consumers;
using Chapar.MassTransit.Filters;
using Chapar.MassTransit.Formatters;
using Chapar.MassTransit.Metrics;
using Chapar.MassTransit.Options;
using Chapar.MassTransit.Outbox;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chapar.MassTransit.Extensions;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/> to register Chapar services
/// backed by MassTransit and RabbitMQ.
/// </summary>
public static class ChaparMassTransitExtensions
{
    /// <summary>
    /// Static callback that additional packages (e.g., Chapar.MassTransit.Outbox) can set
    /// to inject configuration into <c>AddChaparMassTransit</c> without calling
    /// <c>AddMassTransit</c> a second time.
    /// </summary>
    public static Action<ChaparMassTransitOptions>? ConfigureBusRegistrationCallback { get; set; }

    /// <summary>
    /// Registers Chapar using MassTransit with RabbitMQ.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">An action to customize the MassTransit/RabbitMQ options.</param>
    /// <param name="handlerAssemblies">Assemblies to scan for <see cref="IMessageHandler{T}"/> implementations.</param>
    public static IServiceCollection AddChaparMassTransit(this IServiceCollection services,
                                                          Action<ChaparMassTransitOptions> configure,
                                                          params Assembly[] handlerAssemblies)
    {
        var options = new ChaparMassTransitOptions();
        configure?.Invoke(options);

        // Merge any configuration injected via the static callback
        ConfigureBusRegistrationCallback?.Invoke(options);

        // If no assemblies are provided, scan all non‑dynamic loaded assemblies for handlers.
        if (handlerAssemblies.Length == 0)
            handlerAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();

        // Register fallback inbox/outbox stores so consumers function without explicit registration.
        services.TryAddScoped<IInboxStore, NullInboxStore>();
        services.TryAddScoped<IOutboxStore, NullOutboxStore>();

        // Discover all IMessageHandler<T> implementations and separate handlers with custom attributes.
        var (standardConsumers, customQueueConsumers, customExchangeMappings) = DiscoverAndRegisterHandlers(services,
                                                                                                           handlerAssemblies,
                                                                                                           options);

        var allConsumers = standardConsumers
            .Select(x => (x.MessageType, x.HandlerType))
            .Concat(customQueueConsumers.Select(x => (x.MessageType, x.HandlerType)))
            .Concat(customExchangeMappings.Select(x => (x.MessageType, x.HandlerType)))
            .Distinct()
            .ToList();

        services.AddMassTransit(mt =>
        {
            // Invoke the callback for additional bus configuration if provided by the package user.
            options.ConfigureBusRegistration?.Invoke(mt);

            // Register each consumer adapter so MassTransit knows about them.
            foreach (var consumer in allConsumers)
            {
                var adapterType = typeof(ChaparConsumerAdapter<,>)
                    .MakeGenericType(consumer.MessageType, consumer.HandlerType);

                mt.AddConsumer(adapterType);
            }

            mt.SetEndpointNameFormatter(new ChaparEndpointNameFormatter(options.QueueNamePrefix,
                                                                        options.QueueNameSuffix));

            mt.UsingRabbitMq((registrationContext, cfg) =>
            {
                // --- RabbitMQ connection settings ---
                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                // --- Resilience: retry policy ---
                cfg.UseMessageRetry(r => r.Interval(options.Resilience.RetryCount,
                                                    options.Resilience.RetryInterval));

                if (options.Resilience.UseDeadLetter)
                {
                    cfg.UseDelayedRedelivery(r => r.Intervals(
                        Enumerable.Repeat(TimeSpan.FromSeconds(10), options.Resilience.MaxRedelivery).ToArray()));
                }

                // --- Resilience: circuit breaker ---
                if (options.Resilience.CircuitBreakerEnabled)
                {
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TripThreshold = options.Resilience.CircuitBreakerFailureThreshold;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval = options.Resilience.CircuitBreakerResetInterval;
                    });
                }

                cfg.MessageTopology.SetEntityNameFormatter(new ChaparMessageNameFormatter(options));

                // Populate the ambient message context before downstream consumer code runs.
                cfg.UseConsumeFilter(typeof(ChaparConsumeFilter<>), registrationContext);

                // Apply all registered IConsumeFilter implementations (e.g., Inbox filter).
                var consumeFilters = registrationContext.GetServices<IConsumeFilter>();
                if (consumeFilters.Any())
                {
                    cfg.UseConsumeFilter(typeof(ChaparConsumeFilterAdapter<>), registrationContext);
                }

                // Auto‑configure endpoints for standard consumers.
                // If DefaultExchanges are configured, bind all auto‑generated queues to them.
                cfg.ConfigureEndpoints(registrationContext, endpoint =>
                {
                    if (options.DefaultExchanges.Count > 0 &&
                        endpoint is IRabbitMqReceiveEndpointConfigurator rmqEndpoint)
                    {
                        foreach (var exchangeConfig in options.DefaultExchanges)
                        {
                            rmqEndpoint.Bind(exchangeConfig.Name, binding =>
                            {
                                binding.ExchangeType = exchangeConfig.Type.ToString().ToLowerInvariant();
                                binding.RoutingKey = exchangeConfig.RoutingKey ?? "";
                            });
                        }
                    }
                });

                // Handlers with [QueueName] but without [Exchange] – legacy behaviour.
                // DefaultExchanges are NOT applied here because the handler explicitly defines its queue.
                foreach (var consumer in customQueueConsumers)
                {
                    var adapterType = typeof(ChaparConsumerAdapter<,>)
                        .MakeGenericType(consumer.MessageType, consumer.HandlerType);

                    cfg.ReceiveEndpoint(consumer.QueueName, endpoint =>
                    {
                        endpoint.Consumer(adapterType,
                                          type => registrationContext.GetRequiredService(type));
                    });
                }

                // Handlers with [Exchange] – bind their queue to all specified exchanges.
                foreach (var group in customExchangeMappings.GroupBy(x => x.QueueName))
                {
                    var queueName = group.Key;

                    cfg.ReceiveEndpoint(queueName, endpoint =>
                    {
                        // Attach all distinct consumers to the same queue.
                        foreach (var consumer in group
                                                 .Select(x => new { x.MessageType, x.HandlerType })
                                                 .Distinct())
                        {
                            var adapterType = typeof(ChaparConsumerAdapter<,>)
                                .MakeGenericType(consumer.MessageType, consumer.HandlerType);

                            endpoint.Consumer(adapterType,
                                              type => registrationContext.GetRequiredService(type));
                        }

                        // Bind to specified exchanges
                        if (endpoint is IRabbitMqReceiveEndpointConfigurator rmqEndpoint)
                        {
                            foreach (var binding in group)
                            {
                                rmqEndpoint.Bind(binding.ExchangeName, x =>
                                {
                                    x.ExchangeType = binding.ExchangeType.ToString().ToLowerInvariant();
                                    x.RoutingKey = binding.RoutingKey;
                                });
                            }
                        }
                    });
                }

                // Invoke the callback for additional RabbitMQ configuration if provided by the package user.
                options.ConfigureRabbitMq?.Invoke(registrationContext, cfg);
            });
        });

        // Register the core Chapar bus abstraction.
        services.AddScoped<IChaparBus, MassTransitChaparBus>();

        // Register the outbox publisher background service.
        services.AddHostedService<ChaparOutboxPublisher>();

        // Register message context accessor for pipeline behaviors.
        services.TryAddSingleton<IMessageContextAccessor, MessageContextAccessor>();

        // Register metrics for inbox and outbox monitoring.
        services.TryAddSingleton<IInboxMetrics, InboxMetrics>();
        services.TryAddSingleton<IOutboxMetrics, OutboxMetrics>();

        return services;
    }

    /// <summary>
    /// Scans the provided assemblies for implementations of <see cref="IMessageHandler{T}"/>,
    /// registers them in the DI container along with <see cref="ChaparConsumerAdapter{T,THandler}"/>,
    /// and separates them into three categories:
    /// <list type="bullet">
    ///   <item>Standard handlers (no attributes)</item>
    ///   <item>Handlers with only <see cref="QueueNameAttribute"/></item>
    ///   <item>Handlers with <see cref="ExchangeAttribute"/> (optionally with <see cref="QueueNameAttribute"/>)</item>
    /// </list>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the handlers in.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <param name="options">The MassTransit options used for topology naming.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item>Standard message types for default MassTransit endpoints</item>
    ///   <item>Dictionary mapping message types to custom queue names (legacy)</item>
    ///   <item>List of custom exchange mappings (message type, queue name, exchange attribute)</item>
    /// </list>
    /// </returns>
    private static (List<ConsumerDefinition> standardConsumers,
        List<ConsumerQueueDefinition> customQueueConsumers,
        List<ExchangeBindingDefinition> customExchangeMappings)
        DiscoverAndRegisterHandlers(IServiceCollection services,
                                    Assembly[] assemblies,
                                    ChaparMassTransitOptions options)
    {
        var standardConsumers = new List<ConsumerDefinition>();
        var customQueueConsumers = new List<ConsumerQueueDefinition>();
        var customExchangeMappings = new List<ExchangeBindingDefinition>();

        if (assemblies.Length == 0)
            return (standardConsumers, customQueueConsumers, customExchangeMappings);

        // Flatten all closed IMessageHandler<T> implementations from the given assemblies.
        var handlerEntries = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t is { IsAbstract: false, IsInterface: false })
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMessageHandler<>))
                .Select(i => new { HandlerType = t, MessageType = i.GetGenericArguments()[0] }))
            .ToList();

        foreach (var entry in handlerEntries)
        {
            // Register the concrete handler class so it can be resolved by DI.
            services.AddScoped(entry.HandlerType);

            // Register IMessageHandler<T> → handler for explicit injection.
            services.AddScoped(
                typeof(IMessageHandler<>).MakeGenericType(entry.MessageType),
                entry.HandlerType);

            // Build the adapter type that bridges IMessageHandler<T> to MassTransit's IConsumer<T>.
            var adapterType = typeof(ChaparConsumerAdapter<,>).MakeGenericType(entry.MessageType, entry.HandlerType);

            // Register the adapter as IConsumer<T> (required for MassTransit to discover the consumer).
            services.AddScoped(typeof(IConsumer<>).MakeGenericType(entry.MessageType), adapterType);

            // Also register the adapter directly for manual resolution scenarios.
            services.AddScoped(adapterType);

            // Determine the endpoint configuration based on the handler's attributes.
            var exchangeAttrs = entry.HandlerType.GetCustomAttributes<ExchangeAttribute>().ToList();
            var consumeTypesAttribute = entry.HandlerType.GetCustomAttribute<ConsumeMessageTypesAttribute>();
            var queueNameAttribute = entry.HandlerType.GetCustomAttribute<QueueNameAttribute>();

            if (exchangeAttrs.Count > 0 || consumeTypesAttribute is not null)
            {
                // Handler has [Exchange] or message aliases; use explicit queue name when supplied.
                var queueName = queueNameAttribute?.Name
                    ?? ChaparQueueNameFormatter.Format(entry.HandlerType,
                                                       options.QueueNamePrefix,
                                                       options.QueueNameSuffix);

                if (consumeTypesAttribute is not null)
                {
                    foreach (var messageTypeName in consumeTypesAttribute.MessageTypes)
                    {
                        customExchangeMappings.Add(new ExchangeBindingDefinition
                        {
                            MessageType = entry.MessageType,
                            HandlerType = entry.HandlerType,
                            QueueName = queueName,
                            ExchangeName = messageTypeName,
                            ExchangeType = ExchangeType.Fanout,
                            RoutingKey = string.Empty
                        });
                    }
                }

                foreach (var attr in exchangeAttrs)
                {
                    customExchangeMappings.Add(new ExchangeBindingDefinition
                    {
                        MessageType = entry.MessageType,
                        HandlerType = entry.HandlerType,
                        QueueName = queueName,
                        ExchangeName = attr.Name,
                        ExchangeType = attr.Type,
                        RoutingKey = attr.RoutingKey ?? string.Empty
                    });
                }
            }
            else if (queueNameAttribute is not null)
            {
                // Handler has only [QueueName] – legacy behaviour.
                customQueueConsumers.Add(new ConsumerQueueDefinition
                {
                    MessageType = entry.MessageType,
                    HandlerType = entry.HandlerType,
                    QueueName = queueNameAttribute.Name
                });
            }
            else
            {
                // No attributes – standard MassTransit auto‑configure.
                standardConsumers.Add(new ConsumerDefinition
                {
                    MessageType = entry.MessageType,
                    HandlerType = entry.HandlerType
                });
            }
        }

        return (standardConsumers, customQueueConsumers, customExchangeMappings);
    }
}

// ---------- Null Object Patterns ----------

/// <summary>
/// A no‑op inbox store that always succeeds without any persistence.
/// Used as a fallback when no real inbox store is registered.
/// </summary>
internal class NullInboxStore : IInboxStore
{
    /// <inheritdoc />
    public Task<bool> TryReserveAsync(string messageId,
                                      string consumerTypeName,
                                      CancellationToken cancellationToken)
        => Task.FromResult(true);

    /// <inheritdoc />
    public Task<bool> MarkAsProcessedAsync(InboxMessage message, CancellationToken cancellationToken)
        => Task.FromResult(true);
}

/// <summary>
/// A no‑op outbox store that silently discards all messages.
/// Used as a fallback when no real outbox store is registered.
/// </summary>
internal class NullOutboxStore : IOutboxStore
{
    /// <inheritdoc />
    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>(Array.Empty<OutboxMessage>());

    /// <inheritdoc />
    public Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task<int> GetUnprocessedMessagesCountAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(0);
}

/// <summary>
/// Represents a consumer queue binding to a RabbitMQ exchange.
/// </summary>
internal sealed class ExchangeBindingDefinition
{
    /// <summary>
    /// Gets the handled message CLR type.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the concrete handler type.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// Gets the target queue name.
    /// </summary>
    public required string QueueName { get; init; }

    /// <summary>
    /// Gets the exchange name.
    /// </summary>
    public required string ExchangeName { get; init; }

    /// <summary>
    /// Gets the exchange type.
    /// </summary>
    public required ExchangeType ExchangeType { get; init; }

    /// <summary>
    /// Gets the routing key.
    /// </summary>
    public string RoutingKey { get; init; } = string.Empty;
}

/// <summary>
/// Represents a standard consumer registration using MassTransit automatic endpoint configuration.
/// </summary>
internal sealed class ConsumerDefinition
{
    /// <summary>
    /// Gets the handled message type.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the concrete handler type.
    /// </summary>
    public required Type HandlerType { get; init; }
}

/// <summary>
/// Represents a consumer registration with an explicitly configured queue name.
/// </summary>
internal sealed class ConsumerQueueDefinition
{
    /// <summary>
    /// Gets the handled message type.
    /// </summary>
    public required Type MessageType { get; init; }

    /// <summary>
    /// Gets the concrete handler type.
    /// </summary>
    public required Type HandlerType { get; init; }

    /// <summary>
    /// Gets the queue name.
    /// </summary>
    public required string QueueName { get; init; }
}
