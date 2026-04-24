namespace Chapar.Core.Outbox;

/// <summary>
/// Contract for persisting outgoing messages before they are sent to the broker.
/// Each provider (e.g. MassTransit, Zamin) will supply its own implementation.
/// </summary>
public interface IOutboxStore
{
    /// <summary>Saves a single outbox message in the same transaction as the business data.</summary>
    Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all unprocessed outbox messages. The order should be consistent
    /// to preserve causality (usually by <see cref="OutboxMessage.OccurredOn"/>).
    /// </summary>
    Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks an outbox message as successfully published.</summary>
    Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default);
}