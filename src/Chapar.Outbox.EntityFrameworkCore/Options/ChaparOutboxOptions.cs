using Chapar.Core.Abstractions;

namespace Chapar.Outbox.EntityFrameworkCore.Options;

/// <summary>
/// Options for controlling which types of domain events are persisted to the outbox
/// by the <see cref="Interceptors.OutboxInterceptor"/>.
/// </summary>
public sealed class ChaparOutboxOptions
{
    /// <summary>
    /// If <c>true</c>, domain events implementing <see cref="IDomainEvent"/> will be stored in the outbox.
    /// </summary>
    public bool PublishDomainEvents { get; set; }

    /// <summary>
    /// If <c>true</c>, integration events implementing <see cref="IIntegrationEvent"/> will be stored in the outbox.
    /// Default is <c>true</c>.
    /// </summary>
    public bool PublishIntegrationEvents { get; set; } = true;
}