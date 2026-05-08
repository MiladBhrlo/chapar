using MassTransit;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Chapar.MassTransit.HealthChecks;

/// <summary>
/// A health check that reports the status of the Chapar (MassTransit) message bus,
/// including all configured receive endpoints.
/// </summary>
internal sealed class ChaparHealthCheck : IHealthCheck
{
    private readonly IBusControl _busControl;

    public ChaparHealthCheck(IBusControl busControl) => _busControl = busControl;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
                                                    CancellationToken cancellationToken = default)
    {
        var result = _busControl.CheckHealth();

        var data = new Dictionary<string, object>
        {
            ["Endpoints"] = string.Join(", ", result.Endpoints.Select(e => $"{e.Key}: {e.Value}"))
        };

        return Task.FromResult(result.Status switch
        {
            BusHealthStatus.Healthy => HealthCheckResult.Healthy("Bus and all endpoints are healthy.", data),
            BusHealthStatus.Degraded => HealthCheckResult.Degraded("One or more endpoints are degraded.", data: data),
            BusHealthStatus.Unhealthy => HealthCheckResult.Unhealthy("The bus is unhealthy or disconnected.", data: data),
            _ => HealthCheckResult.Unhealthy("Unknown bus health status.", data: data)
        });
    }
}
