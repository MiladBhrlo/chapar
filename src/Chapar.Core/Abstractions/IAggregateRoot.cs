namespace Chapar.Core.Abstractions;

/// <summary>
/// Marks an entity as an aggregate root that can collect domain events.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Gets the domain events that have been raised but not yet dispatched.</summary>
    IReadOnlyCollection<object> DomainEvents { get; }

    /// <summary>Adds a domain event to the aggregate's internal collection.</summary>
    void AddDomainEvent(object domainEvent);

    /// <summary>Clears all domain events, usually after they have been dispatched.</summary>
    void ClearDomainEvents();
}

/// <summary>
/// Base class for aggregate roots with built‑in domain event collection.
/// </summary>
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<object> _domainEvents = [];

    public IReadOnlyCollection<object> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(object domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}