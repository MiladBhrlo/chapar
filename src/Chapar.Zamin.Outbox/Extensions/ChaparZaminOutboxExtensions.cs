using Chapar.Core.Cleanup;
using Chapar.Core.Inbox;
using Chapar.Core.Outbox;
using Chapar.Zamin.Outbox.Cleanup;
using Chapar.Zamin.Outbox.Inbox;
using Chapar.Zamin.Outbox.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Chapar.Zamin.Outbox.Extensions;

/// <summary>
/// Extension methods for configuring Chapar Outbox and Inbox on Zamin infrastructure.
/// </summary>
public static class ChaparZaminOutboxExtensions
{
    /// <summary>
    /// Registers Zamin‑backed implementations for <see cref="IOutboxStore"/> and <see cref="IInboxStore"/>,
    /// and optionally enables automatic cleanup of the outbox table.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureCleanup">Optional action to customize <see cref="CleanupOptions"/> for the outbox table.</param>
    public static IServiceCollection AddChaparZaminOutbox(this IServiceCollection services,
                                                          Action<CleanupOptions>? configureCleanup = null)
    {
        services.AddScoped<IOutboxStore, ZaminOutboxStore>();
        services.AddScoped<IInboxStore, ZaminInboxStore>();

        services.AddZaminOutboxCleanup<ZaminOutboxStore>(configureCleanup);

        return services;
    }

    /// <summary>
    /// Registers a custom cleanup job for the Zamin outbox using the specified store.
    /// </summary>
    /// <typeparam name="TStore">The store type that implements <see cref="ICleanupStore"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the cleanup job.</param>
    public static IServiceCollection AddZaminOutboxCleanup<TStore>(this IServiceCollection services,
                                                                   Action<CleanupOptions>? configure = null)
    where TStore : class, ICleanupStore
    {
        services.TryAddScoped<TStore>();

        services.AddHostedService(sp =>
        {
            var options = new CleanupOptions();
            configure?.Invoke(options);
            return new CleanupBackgroundService<TStore>(
                sp.GetRequiredService<IServiceScopeFactory>(),
                options,
                sp.GetRequiredService<ILogger<CleanupBackgroundService<TStore>>>());
        });

        return services;
    }
}
