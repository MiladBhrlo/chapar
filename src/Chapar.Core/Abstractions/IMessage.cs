namespace Chapar.Core.Abstractions;

/// <summary>
/// Marker interface for all messages flowing through the Chapar bus.
/// There is no constraint on the structure; it exists to enable compile‑time dispatch.
/// </summary>
public interface IMessage { }
