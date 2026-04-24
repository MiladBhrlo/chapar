using Chapar.Core.Abstractions;
using Chapar.Core.Outbox;
using System.Text.Json;

namespace Chapar.Outbox.EntityFrameworkCore.Publishers;

/// <summary>
/// A decorator for <see cref="IChaparBus"/> that stores all outgoing messages
/// in the outbox table instead of sending them directly to the broker.
/// </summary>
internal sealed class OutboxChaparBus : IChaparBus
{
    private readonly IOutboxStore _outboxStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxChaparBus"/> class.
    /// </summary>
    /// <param name="outboxStore">The outbox store used to persist messages.</param>
    public OutboxChaparBus(IOutboxStore outboxStore)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TEvent).AssemblyQualifiedName ?? typeof(TEvent).FullName ?? typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event, typeof(TEvent)),
            OccurredOn = DateTime.UtcNow,
            Headers = headers is not null ? JsonSerializer.Serialize(headers) : null,
            DestinationQueue = null // Events are broadcast
        };

        await _outboxStore.SaveAsync(outboxMessage, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendAsync<TCommand>(TCommand command, string queueName, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TCommand).AssemblyQualifiedName ?? typeof(TCommand).FullName ?? typeof(TCommand).Name,
            Payload = JsonSerializer.Serialize(command, typeof(TCommand)),
            OccurredOn = DateTime.UtcNow,
            Headers = headers is not null ? JsonSerializer.Serialize(headers) : null,
            DestinationQueue = queueName // Commands go to a specific queue
        };

        await _outboxStore.SaveAsync(outboxMessage, cancellationToken);
    }
}