namespace Chapar.Core.Exceptions;

/// <summary>
/// Marker interface for domain-level exceptions that should not be retried.
/// </summary>
public interface IDomainException { }