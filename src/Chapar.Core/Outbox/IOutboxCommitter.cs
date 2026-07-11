namespace Chapar.Core.Outbox;

/// <summary>
/// Optional capability for outbox stores that can commit staged outbox messages immediately.
/// </summary>
public interface IOutboxCommitter
{
    /// <summary>
    /// Commits any staged outbox messages.
    /// </summary>
    Task CommitAsync(CancellationToken cancellationToken = default);
}
