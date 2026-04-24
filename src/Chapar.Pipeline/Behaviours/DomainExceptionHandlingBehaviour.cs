using Chapar.Core.Abstractions;
using Chapar.Core.Exceptions;
using Chapar.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace Chapar.Pipeline.Behaviours;

/// <summary>
/// Catches <see cref="IDomainException"/> and logs them as warnings,
/// preventing them from triggering MassTransit's retry policies.
/// </summary>
public class DomainExceptionHandlingBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<DomainExceptionHandlingBehaviour<TMessage>> _logger;

    public DomainExceptionHandlingBehaviour(ILogger<DomainExceptionHandlingBehaviour<TMessage>> logger) => _logger = logger;

    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        try
        {
            await next();
        }
        catch (Exception ex) when (ex is IDomainException)
        {
            _logger.LogWarning(ex, "A domain exception occurred while processing {MessageType}: {ExceptionMessage}", typeof(TMessage).Name, ex.Message);
            // We swallow the exception here; the message will be ACKed and not retried.
        }
    }
}