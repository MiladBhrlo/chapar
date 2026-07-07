using MassTransit;

namespace Chapar.MassTransit.Options;

/// <summary>
/// Configuration options for the MassTransit‑based Chapar bus.
/// </summary>
public class ChaparMassTransitOptions
{
    /// <summary>RabbitMQ host name or IP address.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>RabbitMQ virtual host (default is "/").</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>Login username.</summary>
    public string Username { get; set; } = "guest";

    /// <summary>Login password.</summary>
    public string Password { get; set; } = "guest";

    /// <summary>
    /// Optional prefix added to generated queue names.
    /// </summary>
    public string? QueueNamePrefix { get; set; }

    /// <summary>
    /// Optional suffix added to generated queue names.
    /// </summary>
    public string? QueueNameSuffix { get; set; }

    /// <summary>
    /// Optional prefix added to generated exchange/entity names.
    /// </summary>
    public string? ExchangeNamePrefix { get; set; }

    /// <summary>
    /// Optional suffix added to generated exchange/entity names.
    /// </summary>
    public string? ExchangeNameSuffix { get; set; }

    /// <summary>
    /// Headers that will be added to every outgoing message unless overridden per message.
    /// Useful for multi‑tenancy, tracing, etc.
    /// </summary>
    public Dictionary<string, object> DefaultHeaders { get; set; } = new();

    /// <summary>
    /// Explicit broker-level message name mappings keyed by message CLR full name.
    /// </summary>
    public IDictionary<string, string> MessageTypeMappings { get; } = new Dictionary<string, string>();

    /// <summary>
    /// Settings for retry and circuit breaker policies applied by MassTransit.
    /// </summary>
    public ResilienceOptions Resilience { get; set; } = new();

    /// <summary>
    /// Default exchanges that will be bound to every consumer queue that does
    /// <b>not</b> carry an explicit <see cref="Chapar.Core.Attributes.ExchangeAttribute"/>
    /// or <see cref="Chapar.Core.Attributes.QueueNameAttribute"/>.
    /// Handlers decorated with those attributes are responsible for their own bindings.
    /// </summary>
    public List<ExchangeConfig> DefaultExchanges { get; set; } = new();

    /// <summary>
    /// An optional callback that allows additional MassTransit configuration
    /// at the bus registration level (e.g., adding Entity Framework outbox).
    /// This is invoked <b>before</b> the RabbitMQ transport is configured.
    /// </summary>
    public Action<IBusRegistrationConfigurator>? ConfigureBusRegistration { get; set; }

    /// <summary>
    /// An optional callback that allows additional MassTransit configuration
    /// at the RabbitMQ transport level.
    /// This is invoked <b>after</b> the standard Chapar configuration.
    /// </summary>
    public Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? ConfigureRabbitMq { get; set; }
}
