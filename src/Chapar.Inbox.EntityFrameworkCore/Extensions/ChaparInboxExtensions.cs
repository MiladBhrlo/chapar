using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using Chapar.Inbox.EntityFrameworkCore.Filters;
using Chapar.Inbox.EntityFrameworkCore.Stores;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Chapar.Inbox.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension methods for configuring the Chapar Inbox on Entity Framework Core.
/// </summary>
public static class ChaparInboxExtensions
{
    /// <summary>
    /// Registers the EF Core‑based inbox services.
    /// </summary>
    public static IServiceCollection AddChaparInboxEntityFramework(this IServiceCollection services)
    {
        services.AddScoped<IInboxStore, EfInboxStore>();
        services.AddScoped<IConsumeFilter, InboxConsumeFilter>();
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
        });

        return builder;
    }
}