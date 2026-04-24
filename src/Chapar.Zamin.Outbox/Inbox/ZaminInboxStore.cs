using Chapar.Core.Inbox;
using Microsoft.EntityFrameworkCore;
using Zamin.Infra.Data.Sql.Commands;

namespace Chapar.Zamin.Outbox.Inbox;

/// <summary>
/// Implements <see cref="IInboxStore"/> using Zamin's <see cref="InboxMessage"/> table.
/// </summary>
public sealed class ZaminInboxStore : IInboxStore
{
    private readonly BaseCommandDbContext _dbContext;

    public ZaminInboxStore(BaseCommandDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsDuplicate(string messageId, string consumerTypeName)
    {
        return await _dbContext.Set<InboxMessage>()
            .AnyAsync(m => m.MessageId == messageId && m.ConsumerTypeName == consumerTypeName);
    }

    public async Task MarkAsProcessedAsync(InboxMessage message)
    {
        var entity = new InboxMessage
        {
            MessageId = message.MessageId,
            ConsumerTypeName = message.ConsumerTypeName,
            ReceivedAt = message.ReceivedAt
        };

        await _dbContext.Set<InboxMessage>().AddAsync(entity);
        await _dbContext.SaveChangesAsync();
    }
}