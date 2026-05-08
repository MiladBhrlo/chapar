using Chapar.Core.Abstractions;
using Chapar.Core.Cleanup;
using Chapar.Core.Inbox;
using Chapar.Inbox.EntityFrameworkCore.Cleanup;
using Chapar.Inbox.EntityFrameworkCore.Filters;
using Chapar.Inbox.EntityFrameworkCore.Options;
using Chapar.Inbox.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chapar.Inbox.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for configuring the Chapar Inbox on Entity Framework Core.
/// </summary>
public static class ChaparInboxExtensions
{
    /// <summary>
    /// Registers the EF Core‑based inbox services and optional delivery behavior, and cleanup job.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">
    /// An optional action to customize <see cref="ChaparInboxOptions"/>,
    /// such as enabling at‑most‑once delivery through
    /// <see cref="ChaparInboxOptions.MarkProcessedAfterFirstAttempt"/>.
    /// </param>
    /// <param name="configureCleanup">Optional action to customize <see cref="CleanupOptions"/> for the inbox table.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddChaparInboxEntityFramework(this IServiceCollection services,
                                                                   Action<ChaparInboxOptions>? configure = null,
                                                                   Action<CleanupOptions>? configureCleanup = null)
    {
        var options = new ChaparInboxOptions();
        configure?.Invoke(options);
        services.Configure<ChaparInboxOptions>(opt =>
        {
            opt.MarkProcessedAfterFirstAttempt = options.MarkProcessedAfterFirstAttempt;
        });

        services.TryAddScoped<EfInboxStore>();
        services.AddScoped<IInboxStore>(sp => sp.GetRequiredService<EfInboxStore>());
        services.AddScoped<IConsumeFilter, InboxConsumeFilter>();

        services.AddInboxCleanup<EfInboxStore>(configureCleanup);

        return services;
    }

    /// <summary>
    /// Configures the inbox table via <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> to configure.</param>
    /// <param name="tableName">The name of the inbox table. Default is "InboxMessages".</param>
    /// <param name="schema">The schema of the inbox table. Default is "chapar".</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    public static ModelBuilder ConfigureChaparInbox(this ModelBuilder builder,
                                                    string tableName = "InboxMessages",
                                                    string schema = "chapar")
    {
        builder.Entity<InboxMessageEntity>(entity =>
        {
            entity.ToTable(tableName, schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageId).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ConsumerTypeName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.ReceivedAt).IsRequired();

            // Unique constraint for atomic reservation
            entity.HasIndex(e => new { e.MessageId, e.ConsumerTypeName }).IsUnique();
        });

        return builder;
    }

    /// <summary>
    /// Registers a custom cleanup job for the inbox using the specified store.
    /// </summary>
    /// <typeparam name="TStore">The store type that implements <see cref="ICleanupStore"/>.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Optional configuration for the cleanup job.</param>
    public static IServiceCollection AddInboxCleanup<TStore>(this IServiceCollection services,
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
