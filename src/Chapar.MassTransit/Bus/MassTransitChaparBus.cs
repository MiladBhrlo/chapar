using Chapar.Core.Abstractions;
using Chapar.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chapar.MassTransit.Bus;

internal sealed class MassTransitChaparBus : IChaparBus
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IOptions<ChaparMassTransitOptions> _options;
    private readonly ILogger<MassTransitChaparBus> _logger;

    public MassTransitChaparBus(IPublishEndpoint publishEndpoint,
                                ISendEndpointProvider sendEndpointProvider,
                                IOptions<ChaparMassTransitOptions> options,
                                ILogger<MassTransitChaparBus> logger)
    {
        _publishEndpoint = publishEndpoint;
        _sendEndpointProvider = sendEndpointProvider;
        _options = options;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent @event,
                                           IDictionary<string, object>? headers = null,
                                           CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        _logger.LogInformation("Publishing event {EventType} ...", typeof(TEvent).Name);
        await _publishEndpoint.Publish(@event, context => ApplyHeaders(context, headers), cancellationToken);
        _logger.LogInformation("Event {EventType} published successfully.", typeof(TEvent).Name);
    }

    public async Task SendAsync<TCommand>(TCommand command,
                                          string queueName,
                                          IDictionary<string, object>? headers = null,
                                          CancellationToken cancellationToken = default)
        where TCommand : class, ICommand
    {
        _logger.LogInformation("Sending command {CommandType} to queue '{QueueName}' ...", typeof(TCommand).Name, queueName);
        var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"queue:{queueName}"));
        await endpoint.Send(command, context => ApplyHeaders(context, headers), cancellationToken);
        _logger.LogInformation("Command {CommandType} sent to queue '{QueueName}'.", typeof(TCommand).Name, queueName);
    }

    private void ApplyHeaders(SendContext context, IDictionary<string, object>? perMessageHeaders)
    {
        // Default headers
        foreach (var kvp in _options.Value.DefaultHeaders)
            context.Headers.Set(kvp.Key, kvp.Value);

        // Per‑message headers (override defaults)
        if (perMessageHeaders is not null)
        {
            foreach (var kvp in perMessageHeaders)
                context.Headers.Set(kvp.Key, kvp.Value);
        }
    }
}