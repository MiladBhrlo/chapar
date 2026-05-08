namespace Chapar.MassTransit.Options;

/// <summary>
/// Retry and circuit breaker settings for the Chapar MassTransit transport.
/// These are applied automatically when using <c>AddChaparMassTransit</c>.
/// </summary>
public sealed class ResilienceOptions
{
    /// <summary>Number of immediate retries when a transient failure occurs.</summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>Interval between immediate retries.</summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Whether the circuit breaker is enabled.</summary>
    public bool CircuitBreakerEnabled { get; set; } = true;

    /// <summary>Failure percentage threshold that trips the circuit breaker.</summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 20;

    /// <summary>Period after which the circuit breaker attempts to reset.</summary>
    public TimeSpan CircuitBreakerResetInterval { get; set; } = TimeSpan.FromMinutes(1);
}
