namespace Chapar.Core.Abstractions;

/// <summary>
/// Marker interface for domain events that are handled internally within the domain.
/// </summary>
public interface IDomainEvent : IMessage { }