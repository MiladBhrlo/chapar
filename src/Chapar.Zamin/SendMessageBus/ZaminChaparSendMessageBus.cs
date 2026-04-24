using Chapar.Core.Abstractions;
using Chapar.Core.Messages;
using Zamin.Extensions.MessageBus.Abstractions;
using Zamin.Extensions.Serializers.Abstractions;

namespace Chapar.Zamin.SendMessageBus;

/// <summary>
/// Implements <see cref="ISendMessageBus"/> by delegating to the Chapar bus.
/// Outbox records (Parcel) are published as <see cref="ParcelMessage"/> events.
/// </summary>
public sealed class ZaminChaparSendMessageBus : ISendMessageBus
{
    private readonly IChaparBus _bus;
    private readonly IJsonSerializer _serializer;

    public ZaminChaparSendMessageBus(IChaparBus bus, IJsonSerializer serializer)
    {
        _bus = bus;
        _serializer = serializer;
    }

    public void Send(Parcel parcel)
    {
        var message = new ParcelMessage
        {
            MessageId = parcel.MessageId,
            MessageName = parcel.MessageName,
            MessageBody = parcel.MessageBody,
            Route = parcel.Route,
            Headers = parcel.Headers
        };

        // Fire‑and‑forget that matches the void signature
        Task.Run(() => _bus.PublishAsync(message));
    }

    public void Publish<TInput>(TInput message)
    {
        var parcel = new Parcel
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageName = typeof(TInput).Name,
            MessageBody = _serializer.Serialize(message),
            Route = $"{AppDomain.CurrentDomain.FriendlyName}.event.{typeof(TInput).Name}",
            Headers = new Dictionary<string, object>()
        };
        Send(parcel);
    }

    public void SendCommandTo<TCommandData>(string destinationService, string commandName, TCommandData commandData)
    {
        var parcel = new Parcel
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageName = commandName,
            MessageBody = _serializer.Serialize(commandData),
            Route = $"{destinationService}.command.{commandName}",
            Headers = new Dictionary<string, object>()
        };
        Send(parcel);
    }

    public void SendCommandTo<TCommandData>(string destinationService, string commandName, string aggregateId, TCommandData commandData)
    {
        var parcel = new Parcel
        {
            MessageId = Guid.NewGuid().ToString(),
            MessageName = commandName,
            MessageBody = _serializer.Serialize(commandData),
            Route = $"{destinationService}.command.{commandName}",
            Headers = new Dictionary<string, object>
            {
                ["AggregateId"] = aggregateId
            }
        };
        Send(parcel);
    }
}