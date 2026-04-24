using Chapar.Core.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Chapar.Inbox.EntityFrameworkCore.Stores;

/// <summary>
/// Implements <see cref="IInboxStore"/> using Entity Framework Core.
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
    public async Task<bool> IsDuplicate(string messageId, string consumerTypeName)
    {
        return await _dbContext.Set<InboxMessageEntity>()
            .AnyAsync(m => m.MessageId == messageId && m.ConsumerTypeName == consumerTypeName);
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(InboxMessage message)
    {
        var entity = new InboxMessageEntity
        {
            MessageId = message.MessageId,
            ConsumerTypeName = message.ConsumerTypeName,
            ReceivedAt = message.ReceivedAt
        };

        await _dbContext.Set<InboxMessageEntity>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }
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
}