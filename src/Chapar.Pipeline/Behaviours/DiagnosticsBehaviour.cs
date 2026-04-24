using Chapar.Core.Abstractions;
using Chapar.Core.Pipeline;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Chapar.Pipeline.Behaviours;

/// <summary>
/// Logs the start, successful completion, and elapsed time of each message handler.
/// </summary>
public class DiagnosticsBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<DiagnosticsBehaviour<TMessage>> _logger;

    public DiagnosticsBehaviour(ILogger<DiagnosticsBehaviour<TMessage>> logger) => _logger = logger;

    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Handling {MessageType} started.", typeof(TMessage).Name);

        try
        {
            await next();
            sw.Stop();
            _logger.LogInformation("Handling {MessageType} completed in {ElapsedMilliseconds}ms.", typeof(TMessage).Name, sw.ElapsedMilliseconds);
        }
        catch
        {
            sw.Stop();
            _logger.LogWarning("Handling {MessageType} failed after {ElapsedMilliseconds}ms.", typeof(TMessage).Name, sw.ElapsedMilliseconds);
            throw;
        }
    }
}