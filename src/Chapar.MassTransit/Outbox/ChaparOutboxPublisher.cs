using System.Text.Json;
using Chapar.Core.Metrics;
using Chapar.Core.Outbox;
using Chapar.MassTransit.Extensions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chapar.MassTransit.Outbox;

/// <summary>
/// A background service that periodically reads pending outbox messages
/// and publishes them to the broker.
/// </summary>
internal sealed class ChaparOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChaparOutboxPublisher> _logger;
    private readonly IBusControl _busControl;
    private readonly IOutboxMetrics? _outboxMetrics;
    private readonly TimeSpan _interval;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaparOutboxPublisher"/> class.
    /// </summary>
    /// <param name="scopeFactory">The scope factory to create scopes for resolving services.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="busControl">The MassTransit bus control instance.</param>
    /// <param name="outboxMetrics">An optional outbox metrics recorder for monitoring published/failed/pending messages.</param>
    /// <param name="interval">An optional interval between polling cycles. Defaults to 5 seconds.</param>
    public ChaparOutboxPublisher(IServiceScopeFactory scopeFactory,
                                 ILogger<ChaparOutboxPublisher> logger,
                                 IBusControl busControl,
                                 IOutboxMetrics? outboxMetrics = null,
                                 TimeSpan? interval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _busControl = busControl;
        _outboxMetrics = outboxMetrics;
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _busControl.WaitForHealthStatus(BusHealthStatus.Healthy, stoppingToken);
        _logger.LogInformation("Chapar outbox publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                if (outboxStore is NullOutboxStore)
                    // MassTransit or another provider is handling the outbox; nothing to do.
                    return;

                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var messages = await outboxStore.GetUnprocessedMessagesAsync(stoppingToken);
                _outboxMetrics?.RecordPendingCount(messages.Count);

                foreach (var outboxMsg in messages)
                {
                    try
                    {
                        var messageType = Type.GetType(outboxMsg.MessageType, throwOnError: true)!;
                        var message = JsonSerializer.Deserialize(outboxMsg.Payload, messageType);
                        if (message is null)
                        {
                            _logger.LogError("Failed to deserialize outbox message {Id}. Payload may be corrupt.", outboxMsg.Id);
                            continue;
                        }

                        var headers = outboxMsg.Headers is not null
                            ? JsonSerializer.Deserialize<Dictionary<string, object>>(outboxMsg.Headers)
                            : null;

                        // Publish with the actual message type
                        await publishEndpoint.Publish(message, messageType, context =>
                        {
                            if (headers is null)
                                return;

                            foreach (var kv in headers)
                            {
                                context.Headers.Set(kv.Key, kv.Value);
                            }
                        }, stoppingToken);

                        await outboxStore.MarkAsProcessedAsync(outboxMsg.Id, stoppingToken);
                        _outboxMetrics?.RecordPublished();
                        var remaining = await outboxStore.GetUnprocessedMessagesCountAsync(stoppingToken);
                        _outboxMetrics?.RecordPendingCount(remaining);

                        _logger.LogInformation("Outbox message {Id} published and marked processed.", outboxMsg.Id);
                    }
                    catch (Exception ex)
                    {
                        _outboxMetrics?.RecordFailed();
                        _logger.LogError(ex, "Failed to publish outbox message {Id}. Will retry later.", outboxMsg.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _outboxMetrics?.RecordFailed();
                _logger.LogError(ex, "Outbox publisher cycle failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}
