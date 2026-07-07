using Chapar.Core.Abstractions;
using Chapar.MassTransit.Adapters;
using MassTransit;

namespace Chapar.MassTransit.Filters;

/// <summary>
/// Populates the ambient <see cref="IMessageContext"/> for the current consumed message.
/// </summary>
public sealed class ChaparConsumeFilter<T> : IFilter<ConsumeContext<T>>
    where T : class
{
    private readonly IMessageContextAccessor _contextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChaparConsumeFilter{T}"/> class.
    /// </summary>
    /// <param name="contextAccessor">The accessor used to set the ambient message context.</param>
    public ChaparConsumeFilter(IMessageContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor ?? throw new ArgumentNullException(nameof(contextAccessor));
    }

    /// <inheritdoc />
    public async Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
    {
        var headers = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var header in context.Headers.GetAll())
        {
            headers[header.Key] = header.Value;
        }

        _contextAccessor.Context = new Chapar.MassTransit.Adapters.MessageContext
        {
            MessageId = context.MessageId?.ToString() ?? Guid.NewGuid().ToString(),
            MessageType = typeof(T).AssemblyQualifiedName ?? typeof(T).FullName ?? typeof(T).Name,
            Headers = headers.AsReadOnly(),
            Message = context.Message
        };

        try
        {
            await next.Send(context);
        }
        finally
        {
            _contextAccessor.Context = null;
        }
    }

    /// <inheritdoc />
    public void Probe(ProbeContext context) => context.CreateFilterScope("ChaparConsumeFilter");
}
