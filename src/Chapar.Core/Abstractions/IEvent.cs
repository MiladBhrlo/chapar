namespace Chapar.Core.Abstractions;

/// <summary>
/// Represents an event that can be broadcast to any number of subscribers.
/// Events are immutable facts that happened in the past.
/// </summary>
public interface IEvent : IMessage { }
