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
    /// Number of times a failed message is retried immediately (interval-based).
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Interval between retries when an immediate retry policy is used.
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether to enable the Circuit Breaker pattern.
    /// </summary>
    public bool CircuitBreakerEnabled { get; set; } = true;

    /// <summary>
    /// Failure percentage threshold that trips the circuit breaker (0‑100).
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 20;

    /// <summary>
    /// Period after which the circuit breaker attempts to reset.
    /// </summary>
    public TimeSpan CircuitBreakerResetInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Headers that will be added to every outgoing message unless overridden per message.
    /// Useful for multi‑tenancy, tracing, etc.
    /// </summary>
    public Dictionary<string, object> DefaultHeaders { get; set; } = new();
}