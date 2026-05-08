namespace Chapar.Core.Cleanup;

/// <summary>
/// A store that supports deleting old processed records.
/// </summary>
public interface ICleanupStore
{
    /// <summary>
    /// Deletes all processed records older than the specified date.
    /// Returns the number of deleted records.
    /// </summary>
    /// <param name="olderThan">Cutoff date; records processed before this will be deleted.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of deleted records.</returns>
    Task<int> DeleteProcessedAsync(DateTime olderThan, CancellationToken cancellationToken = default);
}
