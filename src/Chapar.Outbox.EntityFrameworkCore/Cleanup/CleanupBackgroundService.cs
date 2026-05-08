using Chapar.Core.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chapar.Outbox.EntityFrameworkCore.Cleanup;

/// <summary>
/// Generic background service that periodically deletes old processed outbox records.
/// </summary>
/// <typeparam name="TStore">The type of the store that implements <see cref="ICleanupStore"/>.</typeparam>
internal sealed class CleanupBackgroundService<TStore> : BackgroundService
    where TStore : notnull, ICleanupStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupBackgroundService<TStore>> _logger;
    private readonly CleanupOptions _options;

    public CleanupBackgroundService(IServiceScopeFactory scopeFactory,
                                    CleanupOptions options,
                                    ILogger<CleanupBackgroundService<TStore>> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Outbox cleanup is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<TStore>();
                var cutoff = DateTime.UtcNow - _options.RetentionPeriod;

                var deleted = await store.DeleteProcessedAsync(cutoff, stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation("Outbox cleanup deleted {Count} old records.", deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Outbox cleanup job failed.");
            }

            await Task.Delay(_options.Interval, stoppingToken);
        }
    }
}
