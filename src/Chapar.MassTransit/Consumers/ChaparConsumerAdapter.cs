using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Chapar.MassTransit.Consumers;

/// <summary>
/// Bridges the generic <see cref="IMessageHandler{T}"/> to MassTransit's <see cref="IConsumer{T}"/>,
/// and applies the Inbox pattern for idempotent processing.
/// </summary>
public class ChaparConsumerAdapter<T> : IConsumer<T> where T : class, IMessage
{
    private readonly IMessageHandler<T> _handler;
    private readonly IInboxStore? _inboxStore;
    private readonly ILogger<ChaparConsumerAdapter<T>> _logger;

    public ChaparConsumerAdapter(
        IMessageHandler<T> handler,
        IInboxStore? inboxStore = null,
        ILogger<ChaparConsumerAdapter<T>>? logger = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _inboxStore = inboxStore;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ChaparConsumerAdapter<T>>.Instance;
    }

    public async Task Consume(ConsumeContext<T> context)
    {
        var messageId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();
        var consumerName = _handler.GetType().FullName ?? typeof(T).Name;

        // Idempotency check
        if (_inboxStore is not null)
        {
            if (await _inboxStore.IsDuplicate(messageId, consumerName))
            {
                _logger.LogWarning(
                    "Duplicate message {MessageId} for consumer {ConsumerName} detected. Message will be acknowledged but not processed.",
                    messageId,
                    consumerName);
                return; // Ack without processing
            }
        }

        try
        {
            await _handler.HandleAsync(context.Message, context.CancellationToken);

            // Mark as processed only if handler succeeds
            if (_inboxStore is not null)
            {
                var inboxMessage = new InboxMessageRecord
                {
                    MessageId = messageId,
                    ConsumerTypeName = consumerName,
                    ReceivedAt = DateTime.UtcNow,
                    IsProcessed = true,
                };

                await _inboxStore.MarkAsProcessedAsync(inboxMessage);
            }

            _logger.LogInformation("Message {MessageId} processed successfully by {ConsumerName}.", messageId, consumerName);
        }
        catch
        {
            // If processing fails, do NOT mark as processed. Let MassTransit retry/send to error queue.
            throw;
        }
    }
}

// Internal concrete implementation of InboxMessage (scoped to this adapter)
internal class InboxMessageRecord : InboxMessage
{
    // All properties inherited.
}