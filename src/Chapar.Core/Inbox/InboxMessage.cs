namespace Chapar.Core.Inbox;

/// <summary>
/// Represents a record of an incoming message that has been (or is being) processed.
/// Used by the Inbox pattern to guarantee exactly‑once processing semantics.
/// </summary>
public class InboxMessage
{
    /// <summary>
    /// Database primary key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Transport-level unique message identifier.
    /// Usually corresponds to MassTransit MessageId.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// Fully-qualified consumer handler type name.
    /// </summary>
    public string ConsumerTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Current processing state of the inbox message.
    /// </summary>
    public InboxMessageStatus Status { get; set; }

    /// <summary>
    /// Number of processing attempts performed for this message.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Timestamp when the message was first reserved.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Timestamp of the latest processing attempt.
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }

    /// <summary>
    /// Timestamp when processing completed successfully.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Last captured processing error.
    /// Intended for diagnostics and operational visibility.
    /// </summary>
    public string? LastError { get; set; }
}

/// <summary>
/// Represents the lifecycle state of a persisted inbox message.
/// </summary>
public enum InboxMessageStatus
{
    /// <summary>
    /// Message has been reserved by a consumer and processing is currently in progress.
    /// </summary>
    Reserved = 0,

    /// <summary>
    /// Message has been processed successfully.
    /// No further retries should occur.
    /// </summary>
    Processed = 1,

    /// <summary>
    /// Message processing failed, but the message is still eligible for retry.
    /// </summary>
    Failed = 2,

    /// <summary>
    /// Message permanently failed after all retry attempts were exhausted.
    /// </summary>
    Poisoned = 3
}
