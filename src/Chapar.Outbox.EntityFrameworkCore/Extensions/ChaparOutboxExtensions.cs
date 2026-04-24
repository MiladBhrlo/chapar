using Chapar.Core.Abstractions;
using Chapar.Core.Outbox;
using Chapar.Outbox.EntityFrameworkCore.Interceptors;
using Chapar.Outbox.EntityFrameworkCore.Options;
using Chapar.Outbox.EntityFrameworkCore.Publishers;
using Chapar.Outbox.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chapar.Outbox.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for configuring the Chapar Outbox on Entity Framework Core.
/// </summary>
public static class ChaparOutboxExtensions
{
    /// <summary>
    /// Registers the EF Core‑based outbox services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configure">An optional action to customize <see cref="ChaparOutboxOptions"/>.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddChaparOutboxEntityFramework(
        this IServiceCollection services,
        Action<ChaparOutboxOptions>? configure = null)
    {
        var options = new ChaparOutboxOptions();
        configure?.Invoke(options);

        services.Configure<ChaparOutboxOptions>(opt =>
        {
            opt.PublishDomainEvents = options.PublishDomainEvents;
            opt.PublishIntegrationEvents = options.PublishIntegrationEvents;
        });

        services.AddScoped<IOutboxStore, EfOutboxStore>();
        services.AddScoped<OutboxInterceptor>();

        services.Replace(ServiceDescriptor.Scoped<IChaparBus, OutboxChaparBus>());

        return services;
    }

    /// <summary>
    /// Configures the outbox table via <see cref="ModelBuilder"/>.
    /// </summary>
    /// <param name="builder">The <see cref="ModelBuilder"/> to configure.</param>
    /// <param name="tableName">The name of the outbox table. Default is "OutboxMessages".</param>
    /// <param name="schema">The schema of the outbox table. Default is "chapar".</param>
    /// <returns>The <see cref="ModelBuilder"/> for chaining.</returns>
    public static ModelBuilder ConfigureChaparOutbox(this ModelBuilder builder,
        string tableName = "OutboxMessages",
        string schema = "chapar")
    {
        builder.Entity<OutboxMessageEntity>(entity =>
        {
            entity.ToTable(tableName, schema);
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MessageType).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.OccurredOn).IsRequired();
            entity.Property(e => e.IsProcessed).IsRequired();
            entity.Property(e => e.Headers).HasMaxLength(4000);
            entity.Property(e => e.DestinationQueue).HasMaxLength(256);
        });

        return builder;
    }
}