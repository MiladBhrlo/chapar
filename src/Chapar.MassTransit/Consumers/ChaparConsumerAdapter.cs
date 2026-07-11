using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using Chapar.Core.Metrics;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Chapar.MassTransit.Consumers;

/// <summary>
/// Bridges <see cref="IMessageHandler{T}"/> implementations to MassTransit's <see cref="IConsumer{T}"/> pipeline,
/// applies Inbox-based idempotent processing, and records inbox processing metrics.
/// </summary>
public class ChaparConsumerAdapter<T, THandler> : IConsumer<T>
    where T : class, IMessage
    where THandler : IMessageHandler<T>
{
    private readonly THandler _handler;
    private readonly IInboxStore? _inboxStore;
    private readonly IInboxMetrics? _inboxMetrics;
    private readonly ILogger<ChaparConsumerAdapter<T, THandler>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaparConsumerAdapter{T,THandler}"/> class.
    /// </summary>
    /// <param name="handler">The message handler to invoke upon successful processing.</param>
    /// <param name="inboxStore">An optional inbox store for idempotent message reservation.</param>
    /// <param name="inboxMetrics">An optional inbox metrics recorder for monitoring processed/duplicate/failed messages.</param>
    /// <param name="logger">An optional logger instance.</param>
    public ChaparConsumerAdapter(THandler handler,
                                 IInboxStore? inboxStore = null,
                                 IInboxMetrics? inboxMetrics = null,
                                 ILogger<ChaparConsumerAdapter<T, THandler>>? logger = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _inboxStore = inboxStore;
        _inboxMetrics = inboxMetrics;
        _logger = logger ?? NullLogger<ChaparConsumerAdapter<T, THandler>>.Instance;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<T> context)
    {
        try
        {
            var messageId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();
            var consumerName = typeof(THandler).FullName
                               ?? _handler.GetType().FullName
                               ?? typeof(THandler).Name;

            // Atomically try to reserve the message.
            if (_inboxStore is not null)
            {
                var reserved = await _inboxStore.TryReserveAsync(messageId, consumerName, context.CancellationToken);
                if (!reserved)
                {
                    _inboxMetrics?.RecordDuplicate();
                    _logger.LogWarning("Message {MessageId} already reserved for consumer {ConsumerName}. Skipping.",
                                       messageId,
                                       consumerName);
                    return; // Ack without processing
                }
            }

            await _handler.HandleAsync(context.Message, context.CancellationToken);
            _inboxMetrics?.RecordProcessed();

            // Mark as processed only if handler succeeds
            if (_inboxStore is not null)
            {
                var inboxMessage = new InboxMessage
                {
                    MessageId = messageId,
                    ConsumerTypeName = consumerName,
                    ReceivedAt = DateTime.UtcNow,
                };

                var marked = await _inboxStore.MarkAsProcessedAsync(inboxMessage, context.CancellationToken);
                if (!marked)
                    _logger.LogWarning(
                        "Message {MessageId} was already marked as processed for consumer {ConsumerName}. " +
                        "This indicates a potential race condition or duplicate call.",
                        messageId, consumerName);
                else
                    _logger.LogInformation(
                        "Message {MessageId} marked as processed for consumer {ConsumerName}.",
                        messageId, consumerName);
            }
            else
                _logger.LogInformation(
                    "Message {MessageId} processed successfully by {ConsumerName}.",
                    messageId, consumerName);

        }
        catch (Exception ex)
        {
            _inboxMetrics?.RecordFailed();
            _logger.LogError(ex, "consumer cycle failed.");
            // If processing fails, do NOT mark as processed. Let MassTransit retry/send to error queue.
            throw;
        }
    }
}
