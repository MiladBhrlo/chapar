using Chapar.Core.Cleanup;
using Chapar.Core.Outbox;
using Microsoft.EntityFrameworkCore;
using Zamin.Extensions.Events.Abstractions;
using Zamin.Extensions.Events.Outbox.Dal.EF;

namespace Chapar.Zamin.Outbox.Outbox;

/// <summary>
/// Implements <see cref="IOutboxStore"/>, <see cref="IOutboxCommitter"/>, and <see cref="ICleanupStore"/>
/// using Zamin's native OutBoxEventItem table.
/// </summary>
public sealed class ZaminOutboxStore : IOutboxStore, IOutboxCommitter, ICleanupStore
{
    private readonly BaseOutboxCommandDbContext _dbContext;

    public ZaminOutboxStore(BaseOutboxCommandDbContext dbContext) => _dbContext = dbContext;

    /// <inheritdoc />
    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = new OutBoxEventItem
        {
            EventId = message.Id,
            EventName = message.MessageType,
            EventPayload = message.Payload,
            AccuredOn = message.OccurredOn,
            IsProcessed = false
        };
        await _dbContext.Set<OutBoxEventItem>().AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Set<OutBoxEventItem>()
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.AccuredOn)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new OutboxMessage
        {
            Id = e.EventId,
            MessageType = e.EventName,
            Payload = e.EventPayload,
            OccurredOn = e.AccuredOn,
            IsProcessed = e.IsProcessed
        }).ToList();
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<OutBoxEventItem>()
            .FirstOrDefaultAsync(e => e.EventId == messageId, cancellationToken);
        if (entity is not null)
        {
            entity.IsProcessed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteProcessedAsync(DateTime olderThan,
                                                CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OutBoxEventItem>()
            .Where(m => m.IsProcessed && m.AccuredOn < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUnprocessedMessagesCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OutBoxEventItem>()
            .CountAsync(m => !m.IsProcessed, cancellationToken);
    }
}
