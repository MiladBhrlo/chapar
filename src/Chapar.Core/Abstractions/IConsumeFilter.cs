namespace Chapar.Core.Abstractions;

/// <summary>
/// An abstraction for a filter that is applied to every incoming message before it reaches the handler.
/// Implementations can enforce policies like Idempotency (Inbox pattern), validation, etc.
/// </summary>
public interface IConsumeFilter
{
    /// <summary>
    /// Determines whether a message with the given ID should be processed.
    /// </summary>
    /// <param name="messageId">The unique identifier of the incoming message.</param>
    /// <param name="consumerTypeName">The fully qualified type name of the consumer.</param>
    /// <returns><c>true</c> if the message is new and should be processed; otherwise, <c>false</c>.</returns>
    Task<bool> ShouldProcessAsync(string messageId, string consumerTypeName);

    /// <summary>
    /// Marks a message as successfully processed to prevent duplicate processing.
    /// </summary>
    /// <param name="messageId">The unique identifier of the message.</param>
    /// <param name="consumerTypeName">The fully qualified type name of the consumer.</param>
    Task MarkAsProcessedAsync(string messageId, string consumerTypeName);
}