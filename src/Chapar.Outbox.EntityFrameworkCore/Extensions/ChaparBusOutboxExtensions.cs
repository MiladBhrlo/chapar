using Chapar.Core.Abstractions;
using Chapar.Outbox.EntityFrameworkCore.Publishers;

namespace Chapar.Outbox.EntityFrameworkCore.Extensions;

/// <summary>
/// Extension overloads for controlling EF outbox save behavior per outgoing message.
/// </summary>
public static class ChaparBusOutboxExtensions
{
    /// <summary>
    /// Publishes an event with explicit EF outbox save behavior.
    /// </summary>
    public static Task PublishAsync<TEvent>(this IChaparBus bus,
                                            TEvent @event,
                                            OutboxSaveMode saveMode = OutboxSaveMode.Transactional,
                                            IDictionary<string, object>? headers = null,
                                            CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        ArgumentNullException.ThrowIfNull(bus);

        if (bus is not IOutboxModeAwareChaparBus outboxBus)
            throw new InvalidOperationException(
                "The configured IChaparBus is not the EF outbox bus. " +
                "Call AddChaparOutboxEntityFramework before using outbox save modes.");

        return outboxBus.PublishAsync(@event, saveMode, headers, cancellationToken);
    }

    /// <summary>
    /// Sends a command with explicit EF outbox save behavior.
    /// </summary>
    public static Task SendAsync<TCommand>(this IChaparBus bus,
                                           TCommand command,
                                           string queueName,
                                           OutboxSaveMode saveMode = OutboxSaveMode.Transactional,
                                           IDictionary<string, object>? headers = null,
                                           CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        ArgumentNullException.ThrowIfNull(bus);

        if (bus is not IOutboxModeAwareChaparBus outboxBus)
            throw new InvalidOperationException(
                "The configured IChaparBus is not the EF outbox bus. " +
                "Call AddChaparOutboxEntityFramework before using outbox save modes.");

        return outboxBus.SendAsync(command, queueName, saveMode, headers, cancellationToken);
    }
}
