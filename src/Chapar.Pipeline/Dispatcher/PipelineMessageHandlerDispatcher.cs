using Chapar.Core.Abstractions;
using Chapar.Core.Pipeline;

namespace Chapar.Pipeline.Dispatcher;

/// <summary>
/// A decorator for <see cref="IMessageHandler{T}"/> that executes a pipeline
/// of behaviours before/after the core handler logic.
/// </summary>
internal sealed class PipelineMessageHandlerDispatcher<TMessage> : IMessageHandler<TMessage>
    where TMessage : class, IMessage
{
    private readonly IMessageHandler<TMessage> _inner;
    private readonly IEnumerable<IPipelineBehavior<TMessage>> _behaviours;

    public PipelineMessageHandlerDispatcher(IMessageHandler<TMessage> inner, IEnumerable<IPipelineBehavior<TMessage>> behaviours)
    {
        _inner = inner;
        _behaviours = behaviours;
    }

    public async Task HandleAsync(TMessage message, CancellationToken cancellationToken = default)
    {
        // Build the pipeline: [behaviour1, behaviour2, ..., behaviourN] -> core handler
        Func<Task> coreHandler = () => _inner.HandleAsync(message, cancellationToken);

        foreach (var behaviour in _behaviours.Reverse())
        {
            var next = coreHandler;
            var currentBehaviour = behaviour;
            coreHandler = () => currentBehaviour.HandleAsync(message, next, cancellationToken);
        }

        await coreHandler();
    }
}