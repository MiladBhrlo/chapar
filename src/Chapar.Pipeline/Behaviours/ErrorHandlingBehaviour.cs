using Chapar.Core.Abstractions;
using Chapar.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace Chapar.Pipeline.Behaviours;

/// <summary>
/// A generic error handling behaviour that logs any unhandled exceptions
/// before rethrowing them.
/// </summary>
public class ErrorHandlingBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<ErrorHandlingBehaviour<TMessage>> _logger;

    public ErrorHandlingBehaviour(ILogger<ErrorHandlingBehaviour<TMessage>> logger) => _logger = logger;

    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        try
        {
            await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred while processing {MessageType}.", typeof(TMessage).Name);
            throw; // Let MassTransit's retry / circuit breaker handle it further
        }
    }
}