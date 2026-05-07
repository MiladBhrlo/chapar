using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using Chapar.Inbox.EntityFrameworkCore.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chapar.Inbox.EntityFrameworkCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxStore"/>.
/// Relies on a unique database constraint on (MessageId, ConsumerTypeName) to provide atomic reservations,
/// and supports both at‑most‑once and at‑least‑once delivery via <see cref="ChaparInboxOptions"/>.
/// </summary>
public sealed class EfInboxStore : IInboxStore
{
    private readonly DbContext _dbContext;
    private readonly ChaparInboxOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfInboxStore"/> class.
    /// </summary>
    /// <param name="dbContext">The <see cref="IChaparDbContext "/> used to access the inbox table.</param>
    /// <param name="options">The inbox configuration options (optional; falls back to defaults).</param>
    public EfInboxStore(IChaparDbContext dbContext, IOptions<ChaparInboxOptions>? options = null)
    {
        _dbContext = (DbContext)dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _options = options?.Value ?? new ChaparInboxOptions();
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
            // Record already exists – decide based on delivery semantics.
            if (_options.MarkProcessedAfterFirstAttempt)
            {
                // At-most-once mode: never retry, treat as duplicate immediately
                return false;
            }

            // At‑least‑once mode: attempt to atomically reclaim the record if it was not yet processed.
            // NOTE: In MassTransit, retries for the same message are never concurrent with the original
            // consumer. The previous consumer must have failed and stopped before redelivery occurs.
            // Therefore, reclaiming the record is safe without a lock token.   
            var updated = await _dbContext.Set<InboxMessageEntity>()
                .Where(e => e.MessageId == messageId && e.ConsumerTypeName == consumerTypeName && !e.IsProcessed)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(e => e.ReceivedAt, DateTime.UtcNow),
                    cancellationToken);

            return updated > 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsProcessedAsync(InboxMessage message,
                                                 CancellationToken cancellationToken = default)
    {
        // Atomic conditional update: only mark if not already processed.
        var updated = await _dbContext.Set<InboxMessageEntity>()
            .Where(e => e.MessageId == message.MessageId &&
                         e.ConsumerTypeName == message.ConsumerTypeName &&
                         !e.IsProcessed)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.IsProcessed, true),
                cancellationToken);

        return updated > 0;
    }

    /// <summary>
    /// Checks whether the <see cref="DbUpdateException"/> was caused by a unique constraint violation.
    /// </summary>
    /// <remarks>
    /// Relies on the exception message containing "UNIQUE". For production use, prefer provider‑specific
    /// error codes (e.g. SQL Server 2601/2627, PostgreSQL 23505).
    /// </remarks>
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