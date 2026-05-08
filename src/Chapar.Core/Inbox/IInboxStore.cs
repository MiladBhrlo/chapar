namespace Chapar.Core.Inbox;

/// <summary>
/// Abstraction for storing and tracking incoming message ids to guarantee exactly‑once processing.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Attempts to atomically reserve an incoming message for processing.
    /// When the method returns <c>true</c> the caller is the exclusive processor of the message;
    /// when it returns <c>false</c> the message has already been reserved by another consumer.
    /// </summary>
    /// <param name="messageId">The unique identifier of the incoming message.</param>
    /// <param name="consumerTypeName">The fully qualified type name of the consumer that will handle the message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the reservation was successful; otherwise <c>false</c>.</returns>
    Task<bool> TryReserveAsync(string messageId,
                               string consumerTypeName,
                               CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a previously reserved message as completely processed.
    /// Called only after the handler has finished without exception.
    /// </summary>
    /// <param name="message">The inbox message record that should be updated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the message was marked for the first time; <c>false</c> if it was already processed.</returns>
    Task<bool> MarkAsProcessedAsync(InboxMessage message,
                                    CancellationToken cancellationToken = default);
}
