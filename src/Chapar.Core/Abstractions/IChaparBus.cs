namespace Chapar.Core.Abstractions;

/// <summary>
/// Primary abstraction for sending events and commands onto the message bus.
/// </summary>
public interface IChaparBus
{
    /// <summary>
    /// Publishes an event to all interested subscribers (fan‑out).
    /// </summary>
    /// <typeparam name="TEvent">The type of the event message (must implement <see cref="IEvent"/>).</typeparam>
    /// <param name="event">The event payload.</param>
    /// <param name="headers">Optional headers that will be attached to the outgoing message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task PublishAsync<TEvent>(TEvent @event, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default)
        where TEvent : class, IEvent;

    /// <summary>
    /// Sends a command directly to the specified queue (point‑to‑point).
    /// </summary>
    /// <typeparam name="TCommand">The type of the command message (must implement <see cref="ICommand"/>).</typeparam>
    /// <param name="command">The command payload.</param>
    /// <param name="queueName">The name of the destination queue.</param>
    /// <param name="headers">Optional headers that will be attached to the outgoing message.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task SendAsync<TCommand>(TCommand command, string queueName, IDictionary<string, object>? headers = null, CancellationToken cancellationToken = default)
        where TCommand : class, ICommand;
}