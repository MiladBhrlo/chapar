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
    /// Headers that will be added to every outgoing message unless overridden per message.
    /// Useful for multi‑tenancy, tracing, etc.
    /// </summary>
    public Dictionary<string, object> DefaultHeaders { get; set; } = new();

    /// <summary>
    /// Settings for retry and circuit breaker policies applied by MassTransit.
    /// </summary>
    public ResilienceOptions Resilience { get; set; } = new();
}
