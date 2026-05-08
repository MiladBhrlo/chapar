using System.Diagnostics.Metrics;
using Chapar.Core.Metrics;
using Chapar.Core.Utilities;

namespace Chapar.MassTransit.Metrics;

/// <summary>
/// MassTransit implementation of <see cref="IOutboxMetrics"/> using <see cref="Meter"/>.
/// </summary>
internal sealed class OutboxMetrics : IOutboxMetrics
{
    private readonly Counter<long> _publishedCounter;
    private readonly Counter<long> _failedCounter;
    private readonly ObservableGauge<long> _pendingGauge;
    private long _pendingCount;

    public OutboxMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Chapar", ChaparVersion.Current);

        _publishedCounter = meter.CreateCounter<long>(
            "chapar.outbox.published",
            "messages",
            "Total number of outbox messages successfully published.");

        _failedCounter = meter.CreateCounter<long>(
            "chapar.outbox.failed",
            "messages",
            "Total number of outbox messages that failed to publish.");

        _pendingGauge = meter.CreateObservableGauge(
            "chapar.outbox.pending",
            () => Interlocked.Read(ref _pendingCount),
            "messages",
            "Current number of outbox messages waiting to be published.");
    }

    public void RecordPublished() => _publishedCounter.Add(1);

    public void RecordFailed() => _failedCounter.Add(1);

    public void RecordPendingCount(long count) => Interlocked.Exchange(ref _pendingCount, count);
}
