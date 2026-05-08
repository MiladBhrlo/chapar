namespace Chapar.Core.Abstractions;

/// <summary>
/// Provides access to the current message headers for both reading and writing.
/// This allows pipeline behaviors to inspect and enrich message metadata
/// without coupling to a specific transport.
/// </summary>
public interface IMessageContextAccessor
{
    /// <summary>
    /// Gets or sets the headers of the current message being processed.
    /// Returns null if no message is currently being processed.
    /// </summary>
    IDictionary<string, object?>? Headers { get; set; }
}
