using System.Reflection;
using Chapar.Core.Abstractions;
using Chapar.Core.Attributes;
using Chapar.MassTransit.Options;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chapar.MassTransit.Bus;

/// <summary>
/// The MassTransit implementation of <see cref="IChaparBus"/>.
/// Handles publishing events and sending commands through RabbitMQ,
/// respecting <see cref="ExchangeAttribute"/> when present on messages.
/// </summary>
internal sealed class MassTransitChaparBus : IChaparBus
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ISendEndpointProvider _sendEndpointProvider;
    private readonly IOptions<ChaparMassTransitOptions> _options;
    private readonly ILogger<MassTransitChaparBus> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MassTransitChaparBus"/> class.
    /// </summary>
    /// <param name="publishEndpoint">The MassTransit publish endpoint for fan‑out messages.</param>
    /// <param name="sendEndpointProvider">The MassTransit send endpoint provider for point‑to‑point messages.</param>
    /// <param name="options">The MassTransit options containing host, resilience, and default headers.</param>
    /// <param name="logger">The logger instance.</param>
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

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event,
                                           IDictionary<string, object>? headers = null,
                                           CancellationToken cancellationToken = default)
        where TEvent : class, IEvent
    {
        _logger.LogInformation("Publishing event {EventType} ...", typeof(TEvent).Name);

        // Check if the message has [Exchange] attributes for custom routing
        var exchangeAttributes = typeof(TEvent).GetCustomAttributes<ExchangeAttribute>().ToList();

        if (exchangeAttributes.Count > 0)
        {
            // Send to each specified exchange
            foreach (var attr in exchangeAttributes)
            {
                var endpoint = await _sendEndpointProvider.GetSendEndpoint(new Uri($"exchange:{attr.Name}"));
                await endpoint.Send(@event, context => ApplyHeaders(context, headers), cancellationToken);
                await endpoint.Send(@event, context =>
                {
                    ApplyHeaders(context, headers);

                    // Set the routing key if specified
                    if (!string.IsNullOrEmpty(attr.RoutingKey))
                        context.SetRoutingKey(attr.RoutingKey);
                }, cancellationToken);
            }
        }
        else
            // Default behaviour: publish to fan‑out exchange named after the message type
            await _publishEndpoint.Publish(@event, context => ApplyHeaders(context, headers), cancellationToken);

        _logger.LogInformation("Event {EventType} published successfully.", typeof(TEvent).Name);
    }

    /// <inheritdoc />
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

    /// <summary>
    /// Applies the default headers from <see cref="ChaparMassTransitOptions"/>
    /// and then overwrites them with any per‑message headers.
    /// </summary>
    /// <param name="context">The send context to add headers to.</param>
    /// <param name="perMessageHeaders">Optional per‑message headers that override defaults.</param>
    private void ApplyHeaders(SendContext context, IDictionary<string, object>? perMessageHeaders)
    {
        // Apply default headers from configuration
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
