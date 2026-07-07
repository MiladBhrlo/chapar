namespace Chapar.Core.Abstractions;

/// <summary>
/// Represents the full context of a message that is currently being processed by the bus.
/// </summary>
public interface IMessageContext
{
    /// <summary>
    /// Gets the unique identifier of the message assigned by the transport.
    /// </summary>
    string MessageId { get; }

    /// <summary>
    /// Gets the fully qualified CLR type name of the message.
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets the headers that were attached to the incoming message.
    /// </summary>
    IReadOnlyDictionary<string, object?> Headers { get; }

    /// <summary>
    /// Gets a shared dictionary for data that should flow through the current message pipeline.
    /// </summary>
    IDictionary<object, object?> Items { get; }

    /// <summary>
    /// Gets the deserialized message payload, or <c>null</c> if the message has not been deserialized yet.
    /// </summary>
    object? Message { get; }
}
