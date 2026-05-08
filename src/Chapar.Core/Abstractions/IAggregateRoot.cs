namespace Chapar.Core.Abstractions;

/// <summary>
/// Marks an entity as an aggregate root that can collect domain events.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Gets the domain events that have been raised but not yet dispatched.</summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }

    /// <summary>Adds a domain event to the aggregate's internal collection.</summary>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>Clears all domain events, usually after they have been dispatched.</summary>
    void ClearDomainEvents();
}

/// <summary>
/// Base class for aggregate roots with built‑in domain event collection.
/// </summary>
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <inheritdoc />
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <inheritdoc />
    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <inheritdoc />
    public void ClearDomainEvents() => _domainEvents.Clear();
}
