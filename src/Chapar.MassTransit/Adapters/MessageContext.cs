using Chapar.Core.Abstractions;

namespace Chapar.MassTransit.Adapters;

/// <summary>
/// Default MassTransit-backed message context.
/// </summary>
internal sealed class MessageContext : IMessageContext
{
    /// <inheritdoc />
    public required string MessageId { get; init; }

    /// <inheritdoc />
    public required string MessageType { get; init; }

    /// <inheritdoc />
    public required IReadOnlyDictionary<string, object?> Headers { get; init; }

    /// <inheritdoc />
    public IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

    /// <inheritdoc />
    public object? Message { get; init; }
}
