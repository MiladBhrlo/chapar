namespace Chapar.Core.Abstractions;

/// <summary>
/// Defines a handler for a specific message type.
/// The bus infrastructure will invoke this when a matching message arrives.
/// </summary>
/// <typeparam name="TMessage">The message type (can be an event, a command, etc.).</typeparam>
public interface IMessageHandler<in TMessage> where TMessage : class, IMessage
{
    /// <summary>
    /// Handles the incoming message.
    /// If this method throws, the bus will apply its configured retry / error policies.
    /// </summary>
    /// <param name="message">The received message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task HandleAsync(TMessage message, CancellationToken cancellationToken = default);
}