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

        if (handlerAssemblies.Length == 0)
            handlerAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => !a.IsDynamic).ToArray();

        services.TryAddScoped<IInboxStore, NullInboxStore>();
        services.TryAddScoped<IOutboxStore, NullOutboxStore>();

        var (standardTypes, customQueueMappings) = DiscoverAndRegisterHandlers(services, handlerAssemblies);

        services.AddMassTransit(mt =>
        {
            foreach (var messageType in standardTypes)
            {
                var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(messageType);
                mt.AddConsumer(adapterType);
            }

            mt.UsingRabbitMq((registrationContext, cfg) =>
            {
                cfg.Host(options.Host, options.VirtualHost, h =>
                {
                    h.Username(options.Username);
                    h.Password(options.Password);
                });

                cfg.UseMessageRetry(r => r.Interval(options.Resilience.RetryCount,
                                                    options.Resilience.RetryInterval));
                if (options.Resilience.CircuitBreakerEnabled)
                {
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TripThreshold = options.Resilience.CircuitBreakerFailureThreshold;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval = options.Resilience.CircuitBreakerResetInterval;
                    });
                }

                // Automatically apply all registered IConsumeFilter adapters (like Inbox)
                var consumeFilters = registrationContext.GetServices<IConsumeFilter>();
                if (consumeFilters.Any())
                {
                    cfg.UseConsumeFilter(typeof(ChaparConsumeFilterAdapter<>), registrationContext);
                }

                cfg.ConfigureEndpoints(registrationContext);

                foreach (var (messageType, queueName) in customQueueMappings)
                {
                    var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(messageType);
                    cfg.ReceiveEndpoint(queueName, endpoint =>
                    {
                        endpoint.Consumer(adapterType, type => registrationContext.GetRequiredService(type));
                    });
                }
            });
        });

        services.AddScoped<IChaparBus, MassTransitChaparBus>();
        services.AddHostedService<ChaparOutboxPublisher>();

        services.AddScoped<Adapters.MessageHeaders>();
        services.TryAddScoped<IMessageContextAccessor>(sp => sp.GetRequiredService<Adapters.MessageHeaders>());

        services.TryAddSingleton<IInboxMetrics, InboxMetrics>();
        services.TryAddSingleton<IOutboxMetrics, OutboxMetrics>();

        return services;
    }

    /// <summary>
    /// Scans the provided assemblies for implementations of <see cref="IMessageHandler{T}"/>,
    /// registers them in the DI container along with <see cref="ChaparConsumerAdapter{T}"/>,
    /// and separates them into standard handlers and handlers with a custom queue name.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to register the handlers in.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>
    /// A tuple containing the list of message types that use the standard MassTransit endpoints,
    /// and a dictionary mapping message types to custom queue names when the handler is decorated
    /// with <see cref="QueueNameAttribute"/>.
    /// </returns>
    private static (List<Type> standardTypes, Dictionary<Type, string> customMappings)
        DiscoverAndRegisterHandlers(IServiceCollection services, Assembly[] assemblies)
    {
        var standardTypes = new List<Type>();
        var customMappings = new Dictionary<Type, string>();

        if (assemblies.Length == 0)
            return (standardTypes, customMappings);

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
            // Register the concrete handler class.
            services.AddScoped(entry.HandlerType);

            // Register IMessageHandler<T> → handler.
            services.AddScoped(
                typeof(IMessageHandler<>).MakeGenericType(entry.MessageType),
                entry.HandlerType);

            var adapterType = typeof(ChaparConsumerAdapter<>).MakeGenericType(entry.MessageType);

            // Register the adapter as IConsumer<T> (required by MassTransit)
            services.AddScoped(typeof(IConsumer<>).MakeGenericType(entry.MessageType), adapterType);

            // Also register the adapter directly (useful for manual resolution).
            services.AddScoped(adapterType);

            // Determine whether the handler requests a custom queue name.
            var attr = entry.HandlerType.GetCustomAttribute<QueueNameAttribute>();
            if (attr is not null)
            {
                customMappings[entry.MessageType] = attr.Name;
            }
            else
            {
                standardTypes.Add(entry.MessageType);
            }
        }

        return (standardTypes, customMappings);
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
