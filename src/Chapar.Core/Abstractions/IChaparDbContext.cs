namespace Chapar.Core.Abstractions;

/// <summary>
/// Marker interface for the DbContext used by Chapar stores.
/// Implement this interface on your application's DbContext to resolve ambiguous
/// injections when multiple DbContext types are registered in the DI container.
/// </summary>
public interface IChaparDbContext { }