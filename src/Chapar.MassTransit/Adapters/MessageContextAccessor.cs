using Chapar.Core.Abstractions;

namespace Chapar.MassTransit.Adapters;

/// <summary>
/// Default implementation of <see cref="IMessageContextAccessor"/> backed by <see cref="AsyncLocal{T}"/>.
/// </summary>
internal sealed class MessageContextAccessor : IMessageContextAccessor
{
    private static readonly AsyncLocal<IMessageContext?> Current = new();

    /// <inheritdoc />
    public IMessageContext? Context
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
