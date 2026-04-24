namespace Chapar.Core.Abstractions;

/// <summary>
/// Marker interface for events that cross service boundaries and are published
/// on the message bus (Integration Events).
/// </summary>
public interface IIntegrationEvent : IEvent { }