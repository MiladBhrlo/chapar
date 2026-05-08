using System.Diagnostics;
using Chapar.Core.Abstractions;
using Chapar.Core.Pipeline;
using Microsoft.Extensions.Logging;

namespace Chapar.Pipeline.Behaviours;

/// <summary>
/// Logs the start, successful completion, and elapsed time of each message handler,
/// and also creates a distributed tracing Activity with message details.
/// </summary>
public class DiagnosticsBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<DiagnosticsBehaviour<TMessage>> _logger;
    private static readonly ActivitySource ActivitySource = new("Chapar.Pipeline");

    public DiagnosticsBehaviour(ILogger<DiagnosticsBehaviour<TMessage>> logger) => _logger = logger;

    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageType = typeof(TMessage).Name;
        using var activity = ActivitySource.StartActivity($"Handle {messageType}", ActivityKind.Internal);

        activity?.SetTag("message.type", messageType);
        if (message is IEvent e) activity?.SetTag("messaging.kind", "event");
        if (message is ICommand c) activity?.SetTag("messaging.kind", "command");

        var sw = Stopwatch.StartNew();
        _logger.LogInformation("Handling {MessageType} started.", typeof(TMessage).Name);

        try
        {
            await next();
            sw.Stop();
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Handling {MessageType} completed in {ElapsedMilliseconds}ms.",
                                   typeof(TMessage).Name,
                                   sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            _logger.LogWarning("Handling {MessageType} failed after {ElapsedMilliseconds}ms.",
                               typeof(TMessage).Name,
                               sw.ElapsedMilliseconds);
            throw;
        }
    }
}
