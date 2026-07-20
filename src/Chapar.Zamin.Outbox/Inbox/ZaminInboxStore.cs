using Chapar.Core.Inbox;
using Microsoft.EntityFrameworkCore;
using Zamin.Infra.Data.Sql.Commands;

namespace Chapar.Zamin.Outbox.Inbox;

/// <summary>
/// Implements <see cref="IInboxStore"/> using Zamin's native <see cref="InboxMessage"/> table
/// and <see cref="BaseCommandDbContext"/>.
/// </summary>
public sealed class ZaminInboxStore : IInboxStore
{
    private readonly BaseCommandDbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="ZaminInboxStore"/> class.
    /// </summary>
    /// <param name="dbContext">The Zamin command database context.</param>
    public ZaminInboxStore(BaseCommandDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            _dbContext.Entry(entity).State = EntityState.Detached;

            var updated = await _dbContext.Set<InboxMessage>()
                .Where(m =>
                    m.MessageId == messageId &&
                    m.ConsumerTypeName == consumerTypeName &&
                    m.Status != InboxMessageStatus.Processed)
                .ExecuteUpdateAsync(setters => setters
                        .SetProperty(x => x.Status, InboxMessageStatus.Reserved)
                        .SetProperty(x => x.LastAttemptAt, utcNow)
                        .SetProperty(x => x.RetryCount, x => x.RetryCount + 1),
                    cancellationToken);

            return updated > 0;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        var updated = await _dbContext.Set<InboxMessage>()
            .Where(m =>
                m.MessageId == message.MessageId &&
                m.ConsumerTypeName == message.ConsumerTypeName &&
                m.Status != InboxMessageStatus.Processed)
            .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Status, InboxMessageStatus.Processed)
                    .SetProperty(x => x.ProcessedAt, DateTime.UtcNow)
                    .SetProperty(x => x.LastError, (string?)null),
                cancellationToken);

        return updated > 0;
    }

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
