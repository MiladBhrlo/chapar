namespace Chapar.Core.Outbox;

/// <summary>
/// Represents a message that has been stored locally as part of an outbox
/// and is waiting to be dispatched to the real message broker.
/// </summary>
public class OutboxMessage
{
    /// <summary>Unique identifier of the outbox record (used for de-duplication).</summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>The fully qualified assembly name of the message type.</summary>
    public string MessageType { get; init; } = default!;

    /// <summary>The serialized message body (JSON, XML, etc.).</summary>
    public string Payload { get; init; } = default!;

    /// <summary>Timestamp when the message was created.</summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>Whether the message has been successfully published to the broker.</summary>
    public bool IsProcessed { get; init; }

    /// <summary>The serialized headers (JSON).</summary>
    public string? Headers { get; init; }

    /// <summary>If set, the message is a command and must be sent to this exact queue.</summary>
    public string? DestinationQueue { get; init; }
}