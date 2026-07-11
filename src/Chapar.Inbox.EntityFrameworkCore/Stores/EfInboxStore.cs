using Chapar.Core.Abstractions;
using Chapar.Core.Cleanup;
using Chapar.Core.Inbox;
using Chapar.Inbox.EntityFrameworkCore.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chapar.Inbox.EntityFrameworkCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IInboxStore"/> and <see cref="ICleanupStore"/>.
/// Relies on a unique database constraint on (MessageId, ConsumerTypeName) to provide atomic reservations,
/// and supports both at‑most‑once and at‑least‑once delivery via <see cref="ChaparInboxOptions"/>.
/// </summary>
public sealed class EfInboxStore : IInboxStore, ICleanupStore
{
    private readonly DbContext _dbContext;
    private readonly ChaparInboxOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfInboxStore"/> class.
    /// </summary>
    /// <param name="dbContext">The <see cref="IChaparDbContext"/> used to access the inbox table.</param>
    /// <param name="options">The inbox configuration options (optional; falls back to defaults).</param>
    public EfInboxStore(IChaparDbContext dbContext, IOptions<ChaparInboxOptions>? options = null)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (dbContext is not DbContext typedDbContext)
        {
            throw new ArgumentException(
                $"{nameof(EfInboxStore)} requires an {nameof(IChaparDbContext)} implementation that also inherits from {nameof(DbContext)}.",
                nameof(dbContext));
        }

        _dbContext = typedDbContext;
        _options = options?.Value ?? new ChaparInboxOptions();
    }

    /// <inheritdoc />
    public async Task<bool> TryReserveAsync(string messageId,
                                            string consumerTypeName,
                                            CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        var entity = new InboxMessage
        {
            MessageId = messageId,
            ConsumerTypeName = consumerTypeName,
            Status = InboxMessageStatus.Reserved,
            RetryCount = 0,
            ReceivedAt = utcNow,
            LastAttemptAt = utcNow
        };

        await _dbContext.Set<InboxMessage>().AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true; // Reservation successful – message is new
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _dbContext.Entry(entity).State = EntityState.Detached;

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
            var updated = await _dbContext.Set<InboxMessage>()
                .Where(e =>
                    e.MessageId == messageId &&
                    e.ConsumerTypeName == consumerTypeName &&
                    e.Status != InboxMessageStatus.Processed)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(e => e.Status, InboxMessageStatus.Reserved)
                        .SetProperty(e => e.LastAttemptAt, utcNow)
                        .SetProperty(e => e.RetryCount, e => e.RetryCount + 1),
                    cancellationToken);

            return updated > 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsProcessedAsync(InboxMessage message,
                                                 CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        // Atomic conditional update: only mark if not already processed.
        var updated = await _dbContext.Set<InboxMessage>()
            .Where(e => e.MessageId == message.MessageId &&
                         e.ConsumerTypeName == message.ConsumerTypeName &&
                         e.Status != InboxMessageStatus.Processed)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(e => e.Status, InboxMessageStatus.Processed)
                    .SetProperty(e => e.ProcessedAt, utcNow)
                    .SetProperty(e => e.LastError, (string?)null),
                cancellationToken);

        return updated > 0;
    }

    /// <inheritdoc />
    public async Task<int> DeleteProcessedAsync(DateTime olderThan,
                                                CancellationToken cancellationToken = default)
        => await _dbContext.Set<InboxMessage>()
            .Where(m =>
                m.Status == InboxMessageStatus.Processed &&
                m.ProcessedAt != null &&
                m.ProcessedAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);

    /// <summary>
    /// Checks whether the <see cref="DbUpdateException"/> was caused by a unique constraint violation.
    /// </summary>
    /// <remarks>
    /// Relies on the exception message containing "UNIQUE". For production use, prefer provider‑specific
    /// error codes (e.g. SQL Server 2601/2627, PostgreSQL 23505).
    /// </remarks>
    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => IsSqlServerUniqueConstraintViolation(ex.InnerException)
           || ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsSqlServerUniqueConstraintViolation(Exception? exception)
    {
        var numberProperty = exception?.GetType().GetProperty("Number");
        if (numberProperty?.GetValue(exception) is not int number)
            return false;

        return number is 2601 or 2627;
    }
}

