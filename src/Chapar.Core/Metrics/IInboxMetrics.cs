namespace Chapar.Core.Metrics;

/// <summary>
/// Defines core counter metrics for inbox message processing.
/// </summary>
public interface IInboxMetrics
{
    /// <summary>
    /// Records a message that was successfully processed.
    /// </summary>
    void RecordProcessed();

    /// <summary>
    /// Records a message that was detected as a duplicate and skipped.
    /// </summary>
    void RecordDuplicate();

    /// <summary>
    /// Records a message that failed processing.
    /// </summary>
    void RecordFailed();
}
