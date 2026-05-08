using Chapar.Core.Abstractions;
using Chapar.MassTransit.Consumers;

namespace Chapar.MassTransit.Adapters;

/// <summary>
/// A settable implementation of <see cref="IMessageContextAccessor"/>.
/// The headers are populated by <see cref="ChaparConsumerAdapter{T}"/>
/// during message consumption and can be read/modified by pipeline behaviors.
/// </summary>
public sealed class MessageHeaders : IMessageContextAccessor
{
    /// <inheritdoc />
    public IDictionary<string, object?>? Headers { get; set; }
}
