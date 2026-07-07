namespace Chapar.Core.Abstractions;

/// <summary>
/// Provides access to the current <see cref="IMessageContext"/>.
/// </summary>
public interface IMessageContextAccessor
{
    /// <summary>
    /// Gets or sets the current <see cref="IMessageContext"/>.
    /// Returns <c>null</c> when no message is being processed.
    /// </summary>
    IMessageContext? Context { get; set; }
}
