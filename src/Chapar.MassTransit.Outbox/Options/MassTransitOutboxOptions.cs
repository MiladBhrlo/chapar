namespace Chapar.MassTransit.Outbox.Options;

/// <summary>
/// Configuration options for the MassTransit transactional outbox.
/// Mirrors MassTransit's native options to provide full control when replacing
/// Chapar's custom outbox implementation.
/// </summary>
public class MassTransitOutboxOptions
{
    /// <summary>
    /// How long to keep inbox entries for duplicate detection.
    /// After this period, entries are eligible for cleanup.
    /// Default is 30 minutes.
    /// </summary>
    public TimeSpan DuplicateDetectionWindow { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Number of messages to deliver at a time from the outbox to the broker.
    /// This setting is only effective when <c>UseBusOutbox()</c> is called.
    /// Default is 100.
    /// </summary>
    public int MessageDeliveryLimit { get; set; } = 100;

    /// <summary>
    /// Transport send timeout when delivering messages to the transport.
    /// This setting is only effective when <c>UseBusOutbox()</c> is called.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan MessageDeliveryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cleanup service polling interval for removing expired inbox entries.
    /// Default is 1 minute.
    /// </summary>
    public TimeSpan QueryDelay { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Database query timeout for cleanup and delivery operations.
    /// Default is 30 seconds.
    /// </summary>
    public TimeSpan QueryTimeout { get; set; } = TimeSpan.FromSeconds(30);
}
