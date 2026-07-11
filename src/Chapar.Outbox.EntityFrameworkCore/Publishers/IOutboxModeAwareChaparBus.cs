using Chapar.Core.Abstractions;

namespace Chapar.Outbox.EntityFrameworkCore.Publishers;

internal interface IOutboxModeAwareChaparBus : IChaparBus
{
    Task PublishAsync<TEvent>(TEvent @event,
                              OutboxSaveMode saveMode,
                              IDictionary<string, object>? headers = null,
                              CancellationToken cancellationToken = default)
        where TEvent : class, IEvent;

    Task SendAsync<TCommand>(TCommand command,
                             string queueName,
                             OutboxSaveMode saveMode,
                             IDictionary<string, object>? headers = null,
                             CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;
}
