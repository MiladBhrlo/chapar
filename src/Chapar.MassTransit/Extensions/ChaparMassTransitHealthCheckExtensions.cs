using Chapar.MassTransit.HealthChecks;
using Microsoft.Extensions.DependencyInjection;

namespace Chapar.MassTransit.Extensions;

/// <summary>
/// Extension methods for adding Chapar health checks.
/// </summary>
public static class ChaparMassTransitHealthCheckExtensions
{
    /// <summary>
    /// Registers a health check that reports the status of the MassTransit bus and its endpoints.
    /// </summary>
    /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
    public static IHealthChecksBuilder AddChaparMassTransitHealthCheck(this IHealthChecksBuilder builder)
    {
        return builder.AddCheck<ChaparHealthCheck>("chapar-masstransit",
                                                   tags: new[] { "ready", "masstransit" });
    }
}
