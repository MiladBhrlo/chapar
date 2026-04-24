namespace Chapar.Core.Inbox;

/// <summary>
/// Contract for recording incoming message ids to enable idempotent processing.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Checks whether a message with the given id has already been processed.
    /// If not, the implementation should atomically reserve (or insert) the id
    /// to prevent concurrent duplicates.
    /// </summary>
    /// <returns>True if the message has already been processed; otherwise false.</returns>
    Task<bool> IsDuplicate(string messageId, string consumerTypeName);

    /// <summary>Records a message as successfully processed.</summary>
    Task MarkAsProcessedAsync(InboxMessage message);
}