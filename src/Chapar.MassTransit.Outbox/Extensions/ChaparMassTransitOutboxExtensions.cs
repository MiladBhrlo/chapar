using Chapar.Core.Abstractions;
using Chapar.Core.Outbox;
using Chapar.MassTransit.Extensions;
using Chapar.MassTransit.Outbox.Options;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chapar.MassTransit.Outbox.Extensions;

/// <summary>
/// Extension methods for configuring Chapar to use the native MassTransit outbox
/// instead of Chapar's custom outbox tables.
/// </summary>
public static class ChaparMassTransitOutboxExtensions
{
    /// <summary>
    /// Replaces Chapar's custom outbox with MassTransit's built‑in transactional outbox.
    /// Requires an Entity Framework Core <typeparamref name="TDbContext"/> that will host
    /// the standard MassTransit outbox tables (<c>OutboxMessage</c>, <c>OutboxState</c>, <c>InboxState</c>).
    /// </summary>
    /// <typeparam name="TDbContext">The application's <see cref="DbContext"/> type.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="configure">Optional action to customize MassTransit outbox options.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// Call this method instead of <c>AddChaparOutboxEntityFramework</c>.
    /// Must be called <b>before</b> <c>AddChaparMassTransit</c> so that the callback
    /// is registered before the bus is configured.
    /// No changes are required in the business code that uses <see cref="IChaparBus"/>.
    /// </remarks>
    public static IServiceCollection AddChaparMassTransitOutbox<TDbContext>(this IServiceCollection services,
                                                                            Action<MassTransitOutboxOptions>? configure = null)
        where TDbContext : DbContext
    {
        var options = new MassTransitOutboxOptions();
        configure?.Invoke(options);

        // Register the callback that will be invoked by AddChaparMassTransit.
        // No service removal happens here – MassTransit replaces IPublishEndpoint directly.
        ChaparMassTransitExtensions.ConfigureBusRegistrationCallback = opt =>
        {
            opt.ConfigureBusRegistration = busCfg =>
            {
                busCfg.AddEntityFrameworkOutbox<TDbContext>(outboxCfg =>
                {
                    outboxCfg.DuplicateDetectionWindow = options.DuplicateDetectionWindow;
                    outboxCfg.QueryDelay = options.QueryDelay;
                    outboxCfg.QueryTimeout = options.QueryTimeout;

                    outboxCfg.UseBusOutbox(busOutboxCfg =>
                    {
                        busOutboxCfg.MessageDeliveryLimit = options.MessageDeliveryLimit;
                        busOutboxCfg.MessageDeliveryTimeout = options.MessageDeliveryTimeout;
                    });
                });
            };
        };

        return services;
    }

    /// <summary>
    /// Adds the standard MassTransit outbox tables to the application's <see cref="DbContext"/>.
    /// </summary>
    /// <param name="modelBuilder">The <see cref="ModelBuilder"/> instance.</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    /// <remarks>
    /// Call this method inside your <c>OnModelCreating</c> override to configure
    /// the <c>OutboxMessage</c>, <c>OutboxState</c>, and <c>InboxState</c> tables.
    /// </remarks>
    public static ModelBuilder AddMassTransitOutboxEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        return modelBuilder;
    }
}
