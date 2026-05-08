namespace Chapar.Core.Metrics;

/// <summary>
/// Defines core counter metrics for outbox message processing.
/// </summary>
public interface IOutboxMetrics
{
    /// <summary>
    /// Records an outbox message that was successfully published to the broker.
    /// </summary>
    void RecordPublished();

    /// <summary>
    /// Records an outbox message that failed to be published.
    /// </summary>
    void RecordFailed();

    /// <summary>
    /// Records the current number of outbox messages that are waiting to be published.
    /// This is an observable gauge that is reported periodically.
    /// </summary>
    void RecordPendingCount(long count);
}
