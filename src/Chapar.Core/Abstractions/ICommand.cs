namespace Chapar.Core.Abstractions;

/// <summary>
/// Represents a command that is sent to exactly one logical receiver.
/// Commands express an intent that should be executed once.
/// </summary>
public interface ICommand : IMessage { }