namespace Chapar.Inbox.EntityFrameworkCore.Options;

/// <summary>
/// Configuration options for the Chapar Inbox pattern.
/// </summary>
public sealed class ChaparInboxOptions
{
    /// <summary>
    /// If <c>true</c>, the inbox marks a message as processed immediately after the first delivery attempt,
    /// regardless of whether the handler succeeded or threw an exception.
    /// This disables retries and ensures at‑most‑once delivery semantics.
    /// Default is <c>false</c>, which allows MassTransit retries and expects idempotent handlers.
    /// </summary>
    public bool MarkProcessedAfterFirstAttempt { get; set; } = false;
}