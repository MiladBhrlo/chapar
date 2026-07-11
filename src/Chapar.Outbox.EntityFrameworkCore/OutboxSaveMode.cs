namespace Chapar.Outbox.EntityFrameworkCore;

/// <summary>
/// Controls when an outbox message is committed.
/// </summary>
public enum OutboxSaveMode
{
    /// <summary>
    /// Stage the outbox message and commit it with the caller's unit of work.
    /// </summary>
    Transactional = 0,

    /// <summary>
    /// Commit the outbox message immediately using a separate SaveChanges call.
    /// </summary>
    Immediate = 1
}
