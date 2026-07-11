using Chapar.Core.Abstractions;
using Chapar.Core.Cleanup;
using Chapar.Core.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Chapar.Outbox.EntityFrameworkCore.Stores;

/// <summary>
/// Entity Framework Core implementation of <see cref="IOutboxStore"/> and <see cref="ICleanupStore"/>.
/// </summary>
public sealed class EfOutboxStore : IOutboxStore, IOutboxCommitter, ICleanupStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfOutboxStore"/> class.
    /// </summary>
    /// <param name="dbContext">The <see cref="IChaparDbContext"/> used to access the outbox table.</param>
    public EfOutboxStore(IChaparDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        if (dbContext is not DbContext efDbContext)
        {
            throw new ArgumentException(
                $"The provided {nameof(IChaparDbContext)} implementation must inherit from {nameof(DbContext)}.",
                nameof(dbContext));
        }

        _dbContext = efDbContext;
    }

    /// <inheritdoc />
    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = new OutboxMessage
        {
            Id = message.Id,
            MessageType = message.MessageType,
            Payload = message.Payload,
            OccurredOn = message.OccurredOn,
            IsProcessed = false,
            Headers = message.Headers,
            DestinationQueue = message.DestinationQueue
        };

        await _dbContext.Set<OutboxMessage>().AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Set<OutboxMessage>()
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.OccurredOn)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entities;
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<OutboxMessage>()
            .FirstOrDefaultAsync(e => e.Id == messageId, cancellationToken);

        if (entity is not null)
        {
            entity.IsProcessed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<int> DeleteProcessedAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OutboxMessage>()
            .Where(m => m.IsProcessed && m.OccurredOn < olderThan)
            .ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> GetUnprocessedMessagesCountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Set<OutboxMessage>()
            .CountAsync(m => !m.IsProcessed, cancellationToken);
    }
}
