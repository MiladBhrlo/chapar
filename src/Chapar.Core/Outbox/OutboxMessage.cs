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
    public string MessageType { get; set; } = default!;

    /// <summary>The serialized message body (JSON, XML, etc.).</summary>
    public string Payload { get; set; } = default!;

    /// <summary>Timestamp when the message was created.</summary>
    public DateTime OccurredOn { get; init; } = DateTime.UtcNow;

    /// <summary>Whether the message has been successfully published to the broker.</summary>
    public bool IsProcessed { get; set; }

    /// <summary>The serialized headers (JSON).</summary>
    public string? Headers { get; init; }

    /// <summary>
    /// Optional destination queue used for point-to-point commands.
    /// When null, the message is treated as a publish/fan-out event.
    /// </summary>
    public string? DestinationQueue { get; init; }
}
