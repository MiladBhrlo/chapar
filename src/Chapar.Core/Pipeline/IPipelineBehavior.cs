using Chapar.Core.Abstractions;

namespace Chapar.Core.Pipeline;

/// <summary>
/// Defines a behaviour that can be plugged into the message handling pipeline.
/// Behaviours are executed in a chain around the core handler logic.
/// </summary>
/// <typeparam name="TMessage">The type of the message being handled.</typeparam>
public interface IPipelineBehavior<in TMessage>
    where TMessage : IMessage
{
    /// <summary>
    /// Executes the behaviour and optionally calls the next delegate in the pipeline.
    /// </summary>
    /// <param name="message">The message to process.</param>
    /// <param name="next">A delegate representing the next action in the pipeline.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task HandleAsync(TMessage message, Func<Task> next, CancellationToken cancellationToken = default);
}