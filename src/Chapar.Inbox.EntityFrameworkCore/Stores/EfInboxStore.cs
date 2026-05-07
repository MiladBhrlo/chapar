using Chapar.Core.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Chapar.Inbox.EntityFrameworkCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxStore"/>.
/// Relies on a unique database constraint on (MessageId, ConsumerTypeName) to provide atomic reservations.
/// </summary>
public sealed class EfInboxStore : IInboxStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfInboxStore"/> class.
    /// </summary>
    /// <param name="dbContext">The <see cref="DbContext"/> used to access the inbox table.</param>
    public EfInboxStore(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task<bool> TryReserveAsync(string messageId,
                                            string consumerTypeName,
                                            CancellationToken cancellationToken = default)
    {
        var entity = new InboxMessageEntity
        {
            MessageId = messageId,
            ConsumerTypeName = consumerTypeName,
            ReceivedAt = DateTime.UtcNow
        };

        await _dbContext.Set<InboxMessageEntity>().AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true; // Reservation successful – message is new
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return false; // Duplicate – another consumer already reserved it
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsProcessedAsync(InboxMessage message,
                                                 CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<InboxMessageEntity>()
                        .FirstOrDefaultAsync(e => e.MessageId == message.MessageId
                                                && e.ConsumerTypeName == message.ConsumerTypeName,
                                             cancellationToken);

        if (entity is null)
            return false; // never reserved (should not happen)

        // Direct check and set, since InboxMessageEntity is a plain EF entity
        if (entity.IsProcessed)
            return false; // already processed

        entity.IsProcessed = true;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Checks whether the <see cref="DbUpdateException"/> was caused by a unique constraint violation.
    /// </summary>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}

/// <summary>
/// Entity that maps to the inbox table.
/// </summary>
public class InboxMessageEntity
{
    /// <summary>
    /// Auto‑incremented primary key.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The unique identifier of the consumed message.
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// The fully‑qualified type name of the consumer that processed the message.
    /// </summary>
    public string ConsumerTypeName { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when the message was first received.
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Indicates if the message has already been fully processed.
    /// </summary>
    public bool IsProcessed { get; set; }
}