namespace Chapar.Core.Inbox;

/// <summary>
/// Represents a record of an incoming message that has been (or is being) processed.
/// Used by the Inbox pattern to guarantee exactly‑once processing semantics.
/// </summary>
public class InboxMessage
{
    /// <summary>The unique identifier of the incoming message (usually the MessageId header).</summary>
    public string MessageId { get; init; } = default!;

    /// <summary>The name of the consumer that processed the message.</summary>
    public string ConsumerTypeName { get; init; } = default!;

    /// <summary>Timestamp when the message was first received.</summary>
    public DateTime ReceivedAt { get; init; } = DateTime.UtcNow;

    /// <summary>Indicates if the message has already been fully processed.</summary>
    public bool IsProcessed { get; init; }
}