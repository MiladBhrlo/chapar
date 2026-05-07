namespace Chapar.Inbox.EntityFrameworkCore.Options;

/// <summary>
/// Configuration options for the Chapar Inbox pattern.
/// </summary>
public sealed class ChaparInboxOptions
{
    /// <summary>
    /// If <c>true</c>, a message that has already been seen (duplicate) is immediately treated as
    /// processed and will not be reservable again. This effectively disables retries and provides
    /// at‑most‑once delivery semantics.
    /// If <c>false</c> (the default), an orphaned reservation (where <c>IsProcessed</c> is still
    /// <c>false</c>) can be reclaimed atomically, allowing MassTransit to retry the message and
    /// expecting an idempotent handler.
    /// </summary>
    public bool MarkProcessedAfterFirstAttempt { get; set; } = false;
}