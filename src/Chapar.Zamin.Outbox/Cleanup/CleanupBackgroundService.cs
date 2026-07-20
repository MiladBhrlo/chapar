using Chapar.Core.Cleanup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Chapar.Zamin.Outbox.Cleanup;

/// <summary>
/// Periodically deletes old processed records from the inbox table using a store that implements <see cref="ICleanupStore"/>.
/// The cleanup interval and retention period are read from named <see cref="CleanupOptions"/> associated with <typeparamref name="TStore"/>.
/// </summary>
/// <typeparam name="TStore">The type of the store that implements <see cref="ICleanupStore"/>.</typeparam>
internal sealed class CleanupBackgroundService<TStore> : BackgroundService
    where TStore : notnull, ICleanupStore
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CleanupBackgroundService<TStore>> _logger;
    private readonly IOptionsMonitor<CleanupOptions> _optionsMonitor;

    public CleanupBackgroundService(IServiceScopeFactory scopeFactory,
                                    ILogger<CleanupBackgroundService<TStore>> logger,
                                    IOptionsMonitor<CleanupOptions> optionsMonitor)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _optionsMonitor.Get(typeof(TStore).FullName!);
        if (!options.Enabled)
        {
            _logger.LogInformation("Cleanup for {StoreType} is disabled.", typeof(TStore).Name);
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var store = scope.ServiceProvider.GetRequiredService<TStore>();
                var cutoff = DateTime.UtcNow - options.RetentionPeriod;

                var deleted = await store.DeleteProcessedAsync(cutoff, stoppingToken);
                if (deleted > 0)
                {
                    _logger.LogInformation("{StoreType} cleanup deleted {Count} old records.", typeof(TStore).Name, deleted);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "{StoreType} cleanup job failed.", typeof(TStore).Name);
            }

            await Task.Delay(options.Interval, stoppingToken);
        }
    }
}
