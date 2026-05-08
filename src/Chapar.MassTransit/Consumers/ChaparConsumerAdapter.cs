using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using Chapar.Core.Metrics;
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
    private readonly IInboxMetrics? _inboxMetrics;
    private readonly Adapters.MessageHeaders? _contextAccessor;
    private readonly ILogger<ChaparConsumerAdapter<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaparConsumerAdapter{T}"/> class.
    /// </summary>
    /// <param name="handler">The message handler to invoke upon successful processing.</param>
    /// <param name="inboxStore">An optional inbox store for idempotent message reservation.</param>
    /// <param name="inboxMetrics">An optional inbox metrics recorder for monitoring processed/duplicate/failed messages.</param>
    /// <param name="contextAccessor">An optional message context accessor used to store headers for pipeline behaviors.</param>
    /// <param name="logger">An optional logger instance.</param>
    public ChaparConsumerAdapter(IMessageHandler<T> handler,
                                 IInboxStore? inboxStore = null,
                                 IInboxMetrics? inboxMetrics = null,
                                 Adapters.MessageHeaders? contextAccessor = null,
                                 ILogger<ChaparConsumerAdapter<T>>? logger = null)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _inboxStore = inboxStore;
        _inboxMetrics = inboxMetrics;
        _contextAccessor = contextAccessor;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<ChaparConsumerAdapter<T>>.Instance;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<T> context)
    {
        // Store headers for pipeline behaviors (e.g., OriginValidation, TenantPropagation)
        if (_contextAccessor is Adapters.MessageHeaders accessor)
        {
            var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in context.Headers.GetAll())
            {
                headers[header.Key] = header.Value;
            }
            accessor.Headers = headers;
        }

        var messageId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString();
        var consumerName = _handler.GetType().FullName ?? typeof(T).Name;

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

        try
        {
            await _handler.HandleAsync(context.Message, context.CancellationToken);
            _inboxMetrics?.RecordProcessed();

            // Mark as processed only if handler succeeds
            if (_inboxStore is not null)
            {
                var inboxMessage = new InboxMessageRecord
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
                    _logger.LogWarning(
                        "Message {MessageId} could not be marked as processed for consumer {ConsumerName}. " +
                        "It may have been processed already.",
                        messageId, consumerName);
            }
            else
                _logger.LogInformation(
                    "Message {MessageId} processed successfully by {ConsumerName}.",
                    messageId, consumerName);

            _logger.LogInformation("Message {MessageId} processed successfully by {ConsumerName}.",
                                   messageId,
                                   consumerName);
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

// Internal concrete implementation of InboxMessage (scoped to this adapter)
internal class InboxMessageRecord : InboxMessage
{
    // All properties inherited.
}
