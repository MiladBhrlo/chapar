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
        var entity = new InboxMessage
        {
            MessageId = messageId,
            ConsumerTypeName = consumerTypeName,
            ReceivedAt = DateTime.UtcNow
        };

        await _dbContext.Set<InboxMessage>().AddAsync(entity, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkAsProcessedAsync(InboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<InboxMessage>()
            .FirstOrDefaultAsync(
                m => m.MessageId == message.MessageId &&
                     m.ConsumerTypeName == message.ConsumerTypeName,
                cancellationToken);

        if (entity is null)
            return false;

        if (!entity.TryMarkAsProcessed())
            return false;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message?.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
