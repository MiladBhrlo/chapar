using Chapar.Core.Cleanup;
using Chapar.Core.Inbox;
using Chapar.Core.Outbox;
using Chapar.Zamin.Outbox.Cleanup;
using Chapar.Zamin.Outbox.Inbox;
using Chapar.Zamin.Outbox.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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
        services.AddScoped<IInboxStore, ZaminInboxStore>();

        services.TryAddScoped<ZaminOutboxStore>();
        services.AddScoped<IOutboxStore>(sp => sp.GetRequiredService<ZaminOutboxStore>());
        services.AddScoped<IOutboxCommitter>(sp => sp.GetRequiredService<ZaminOutboxStore>());

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
        // named options for this specific store
        services.Configure<CleanupOptions>(typeof(TStore).FullName!, opt =>
        {
            if (configure != null)
            {
                // apply custom settings over defaults
                var custom = new CleanupOptions();
                configure(custom);
                opt.Enabled = custom.Enabled;
                opt.RetentionPeriod = custom.RetentionPeriod;
                opt.Interval = custom.Interval;
            }
        });

        // register the background service idempotently
        services.TryAddSingleton<CleanupBackgroundService<TStore>>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, CleanupBackgroundService<TStore>>());

        return services;
    }
}
