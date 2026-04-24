using System.Reflection;
using Chapar.Core.Abstractions;
using Chapar.Core.Attributes;
using Chapar.Core.Inbox;
using Chapar.Core.Outbox;
using Chapar.MassTransit.Bus;
using Chapar.MassTransit.Consumers;
using Chapar.MassTransit.Filters;
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

                cfg.UseMessageRetry(r => r.Interval(options.RetryCount, options.RetryInterval));
                if (options.CircuitBreakerEnabled)
                {
                    cfg.UseCircuitBreaker(cb =>
                    {
                        cb.TripThreshold = options.CircuitBreakerFailureThreshold;
                        cb.ActiveThreshold = 10;
                        cb.ResetInterval = options.CircuitBreakerResetInterval;
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

internal class NullInboxStore : IInboxStore
{
    public Task<bool> IsDuplicate(string messageId, string consumerTypeName)
        => Task.FromResult(false);

    public Task MarkAsProcessedAsync(InboxMessage message)
        => Task.CompletedTask;
}

internal class NullOutboxStore : IOutboxStore
{
    public Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OutboxMessage>>(Array.Empty<OutboxMessage>());

    public Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}