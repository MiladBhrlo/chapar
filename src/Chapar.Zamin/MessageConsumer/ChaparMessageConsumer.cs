using Chapar.Core.Abstractions;
using Chapar.Core.Messages;
using Zamin.Core.Contracts.ApplicationServices.Events;
using Zamin.Core.Domain.Events;
using Zamin.Extensions.MessageBus.Abstractions;
using Zamin.Extensions.Serializers.Abstractions;
using Microsoft.Extensions.Logging;

namespace Chapar.Zamin.Consumer;

/// <summary>
/// Consumes <see cref="ParcelMessage"/> events published by the outbox,
/// deserializes the inner domain event, and dispatches it using Zamin's <see cref="IEventDispatcher"/>.
/// Inbox idempotency is enforced via <see cref="IMessageInboxItemRepository"/>.
/// </summary>
public sealed class ChaparMessageConsumer : IMessageHandler<ParcelMessage>
{
    private readonly IEventDispatcher _dispatcher;
    private readonly IMessageInboxItemRepository _inbox;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<ChaparMessageConsumer> _logger;
    private readonly List<Type> _eventTypes;

    public ChaparMessageConsumer(IEventDispatcher dispatcher,
                                 IMessageInboxItemRepository inbox,
                                 IJsonSerializer serializer,
                                 ILogger<ChaparMessageConsumer> logger)
    {
        _dispatcher = dispatcher;
        _inbox = inbox;
        _serializer = serializer;
        _logger = logger;

        _eventTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => typeof(IDomainEvent).IsAssignableFrom(t) && t.IsClass)
            .ToList();
    }

    public async Task HandleAsync(ParcelMessage message, CancellationToken cancellationToken = default)
    {
        var sender = message.Route?.Split('.')[0] ?? "unknown";

        _logger.LogInformation("📥 ChaparMessageConsumer received message {MessageId} with name '{EventName}'", message.MessageId, message.MessageName);

        if (!_inbox.AllowReceive(message.MessageId, sender))
        {
            _logger.LogWarning("Duplicate message {MessageId} ignored.", message.MessageId);
            return;
        }

        var eventType = _eventTypes.FirstOrDefault(t => t.Name == message.MessageName);
        if (eventType is null)
        {
            _logger.LogWarning("Unknown domain event type: {EventName}. Available types: {Types}",
                message.MessageName, string.Join(", ", _eventTypes.Select(t => t.Name)));
            _inbox.Receive(message.MessageId, sender, message.MessageBody);
            return;
        }

        _logger.LogInformation("Deserializing to {EventType}", eventType.Name);
        dynamic @event = _serializer.Deserialize(message.MessageBody, eventType);
        await _dispatcher.PublishDomainEventAsync(@event);
        _logger.LogInformation("✅ Dispatched domain event {EventName}", message.MessageName);
        _inbox.Receive(message.MessageId, sender, message.MessageBody);
    }
}