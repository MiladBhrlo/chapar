using Chapar.Core.Outbox;
using Microsoft.EntityFrameworkCore;

namespace Chapar.Outbox.EntityFrameworkCore.Stores;

/// <summary>
/// Implements <see cref="IOutboxStore"/> using Entity Framework Core.
/// </summary>
public sealed class EfOutboxStore : IOutboxStore
{
    private readonly DbContext _dbContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfOutboxStore"/> class.
    /// </summary>
    /// <param name="dbContext">The <see cref="DbContext"/> used to access the outbox table.</param>
    public EfOutboxStore(DbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc />
    public async Task SaveAsync(OutboxMessage message, CancellationToken cancellationToken = default)
    {
        var entity = new OutboxMessageEntity
        {
            Id = message.Id,
            MessageType = message.MessageType,
            Payload = message.Payload,
            OccurredOn = message.OccurredOn,
            IsProcessed = false,
            Headers = message.Headers,
            DestinationQueue = message.DestinationQueue
        };

        await _dbContext.Set<OutboxMessageEntity>().AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> GetUnprocessedMessagesAsync(CancellationToken cancellationToken = default)
    {
        var entities = await _dbContext.Set<OutboxMessageEntity>()
            .Where(e => !e.IsProcessed)
            .OrderBy(e => e.OccurredOn)
            .Take(100)
            .ToListAsync(cancellationToken);

        return entities.Select(e => new OutboxMessage
        {
            Id = e.Id,
            MessageType = e.MessageType,
            Payload = e.Payload,
            OccurredOn = e.OccurredOn,
            IsProcessed = e.IsProcessed,
            Headers = e.Headers,
            DestinationQueue = e.DestinationQueue
        }).ToList();
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.Set<OutboxMessageEntity>()
            .FirstOrDefaultAsync(e => e.Id == messageId, cancellationToken);

        if (entity is not null)
        {
            entity.IsProcessed = true;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}

/// <summary>
/// Entity that maps to the outbox table.
/// </summary>
public class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredOn { get; set; }
    public bool IsProcessed { get; set; }
    public string? Headers { get; set; }
    public string? DestinationQueue { get; set; }
}