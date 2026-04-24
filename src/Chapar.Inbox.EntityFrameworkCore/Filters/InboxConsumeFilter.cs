using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using Microsoft.Extensions.Logging;

namespace Chapar.Inbox.EntityFrameworkCore.Filters;

/// <summary>
/// Implements <see cref="IConsumeFilter"/> by using the inbox table to ensure idempotent message processing.
/// </summary>
internal sealed class InboxConsumeFilter : IConsumeFilter
{
    private readonly IInboxStore _inbox;
    private readonly ILogger<InboxConsumeFilter> _logger;

    public InboxConsumeFilter(IInboxStore inbox, ILogger<InboxConsumeFilter> logger)
    {
        _inbox = inbox;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> ShouldProcessAsync(string messageId, string consumerTypeName)
    {
        if (await _inbox.IsDuplicate(messageId, consumerTypeName))
        {
            _logger.LogWarning("Duplicate message {MessageId} for consumer {ConsumerTypeName} skipped.", messageId, consumerTypeName);
            return false;
        }
        return true;
    }

    /// <inheritdoc />
    public async Task MarkAsProcessedAsync(string messageId, string consumerTypeName)
    {
        var inboxMessage = new InboxMessage
        {
            MessageId = messageId,
            ConsumerTypeName = consumerTypeName,
            ReceivedAt = DateTime.UtcNow
        };
        await _inbox.MarkAsProcessedAsync(inboxMessage);
        _logger.LogDebug("Message {MessageId} marked as processed for consumer {ConsumerTypeName}.", messageId, consumerTypeName);
    }
}