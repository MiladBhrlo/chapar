using System.Diagnostics.Metrics;
using Chapar.Core.Metrics;
using Chapar.Core.Utilities;

namespace Chapar.MassTransit.Metrics;

/// <summary>
/// MassTransit implementation of <see cref="IInboxMetrics"/> using <see cref="Meter"/>.
/// </summary>
internal sealed class InboxMetrics : IInboxMetrics
{
    private readonly Counter<long> _processedCounter;
    private readonly Counter<long> _duplicateCounter;
    private readonly Counter<long> _failedCounter;

    public InboxMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Chapar", ChaparVersion.Current);

        _processedCounter = meter.CreateCounter<long>(
            "chapar.inbox.processed",
            "messages",
            "Total number of successfully processed incoming messages.");

        _duplicateCounter = meter.CreateCounter<long>(
            "chapar.inbox.duplicate",
            "messages",
            "Total number of duplicate incoming messages that were skipped.");

        _failedCounter = meter.CreateCounter<long>(
            "chapar.inbox.failed",
            "messages",
            "Total number of incoming messages that failed processing.");
    }

    public void RecordProcessed() => _processedCounter.Add(1);
    public void RecordDuplicate() => _duplicateCounter.Add(1);
    public void RecordFailed() => _failedCounter.Add(1);
}
