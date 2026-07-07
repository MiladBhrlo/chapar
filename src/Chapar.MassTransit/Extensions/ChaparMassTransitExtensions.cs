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
        var (standardTypes, customQueueMappings, customExchangeMappings) = DiscoverAndRegisterHandlers(services, handlerAssemblies);

        services.AddMassTransit(mt =>
        {
            // Invoke the callback for additional bus configuration if provided by the package user.
            options.ConfigureBusRegistration?.Invoke(mt);

            // Register each standard consumer adapter so MassTransit knows about them.
            foreach (var messageType in standardTypes)
            {
                var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(messageType);
                mt.AddConsumer(adapterType);
            }

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
                foreach (var (messageType, queueName) in customQueueMappings)
                {
                    var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(messageType);
                    cfg.ReceiveEndpoint(queueName, endpoint =>
                    {
                        endpoint.Consumer(adapterType, type => registrationContext.GetRequiredService(type));
                    });
                }

                // Handlers with [Exchange] – bind their queue to all specified exchanges.
                foreach (var group in customExchangeMappings.GroupBy(x => x.QueueName))
                {
                    var queueName = group.Key;
                    var distinctMessageTypes = group.Select(x => x.MessageType).Distinct();

                    cfg.ReceiveEndpoint(queueName, endpoint =>
                    {
                        // Attach consumers for each distinct message type
                        foreach (var messageType in distinctMessageTypes)
                        {
                            var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(messageType);
                            endpoint.Consumer(adapterType, type => registrationContext.GetRequiredService(type));
                        }

                        // Bind to specified exchanges
                        if (endpoint is IRabbitMqReceiveEndpointConfigurator rmqEndpoint)
                        {
                            foreach (var (_, _, exchangeAttr) in group)
                            {
                                rmqEndpoint.Bind(exchangeAttr.Name, binding =>
                                {
                                    binding.ExchangeType = exchangeAttr.Type.ToString().ToLowerInvariant();
                                    binding.RoutingKey = exchangeAttr.RoutingKey ?? "";
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
    /// registers them in the DI container along with <see cref="ChaparConsumerAdapter{T}"/>,
    /// and separates them into three categories:
    /// <list type="bullet">
    ///   <item>Standard handlers (no attributes)</item>
    ///   <item>Handlers with only <see cref="QueueNameAttribute"/></item>
    ///   <item>Handlers with <see cref="ExchangeAttribute"/> (optionally with <see cref="QueueNameAttribute"/>)</item>
    /// </list>
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the handlers in.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    ///   <item>Standard message types for default MassTransit endpoints</item>
    ///   <item>Dictionary mapping message types to custom queue names (legacy)</item>
    ///   <item>List of custom exchange mappings (message type, queue name, exchange attribute)</item>
    /// </list>
    /// </returns>
    private static (List<Type> standardTypes,
        Dictionary<Type, string> customQueueMappings,
        List<(Type MessageType, string QueueName, ExchangeAttribute Exchange)> customExchangeMappings)
        DiscoverAndRegisterHandlers(IServiceCollection services,
                                    Assembly[] assemblies)
    {
        var standardTypes = new List<Type>();
        var customQueueMappings = new Dictionary<Type, string>();
        var customExchangeMappings = new List<(Type, string, ExchangeAttribute)>();

        if (assemblies.Length == 0)
            return (standardTypes, customQueueMappings, customExchangeMappings);

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
            var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(entry.MessageType);

            // Register the adapter as IConsumer<T> (required for MassTransit to discover the consumer).
            services.AddScoped(typeof(IConsumer<>).MakeGenericType(entry.MessageType), adapterType);

            // Also register the adapter directly for manual resolution scenarios.
            services.AddScoped(adapterType);

            // Determine the endpoint configuration based on the handler's attributes.
            var exchangeAttrs = entry.HandlerType.GetCustomAttributes<ExchangeAttribute>().ToList();
            if (exchangeAttrs.Count > 0)
            {
                // Handler has [Exchange] – add to custom exchange mappings.
                // Queue name comes from [QueueName] if present, otherwise use the message type name.
                var queueName = entry.HandlerType.GetCustomAttribute<QueueNameAttribute>()?.Name
                                ?? entry.MessageType.FullName!;

                foreach (var attr in exchangeAttrs)
                {
                    customExchangeMappings.Add((entry.MessageType, queueName, attr));
                }
            }
            else if (entry.HandlerType.GetCustomAttribute<QueueNameAttribute>() is { } queueAttr)
            {
                // Handler has only [QueueName] – legacy behaviour.
                customQueueMappings[entry.MessageType] = queueAttr.Name;
            }
            else
            {
                // No attributes – standard MassTransit auto‑configure.
                standardTypes.Add(entry.MessageType);
            }
        }

        return (standardTypes, customQueueMappings, customExchangeMappings);
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
