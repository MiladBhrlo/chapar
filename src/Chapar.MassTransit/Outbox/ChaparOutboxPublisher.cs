using Chapar.Core.Outbox;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Chapar.MassTransit.Outbox;

/// <summary>
/// A background service that periodically reads pending outbox messages
/// and publishes them to the broker.
/// </summary>
internal sealed class ChaparOutboxPublisher : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ChaparOutboxPublisher> _logger;
    private readonly TimeSpan _interval;

    public ChaparOutboxPublisher(IServiceScopeFactory scopeFactory, ILogger<ChaparOutboxPublisher> logger, TimeSpan? interval = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _interval = interval ?? TimeSpan.FromSeconds(5);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var outboxStore = scope.ServiceProvider.GetRequiredService<IOutboxStore>();
                var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

                var messages = await outboxStore.GetUnprocessedMessagesAsync(stoppingToken);

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

                        // Publish with the actual message type
                        await publishEndpoint.Publish(message, messageType, stoppingToken);

                        await outboxStore.MarkAsProcessedAsync(outboxMsg.Id, stoppingToken);
                        _logger.LogInformation("Outbox message {Id} published and marked processed.", outboxMsg.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to publish outbox message {Id}. Will retry later.", outboxMsg.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox publisher cycle failed.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}