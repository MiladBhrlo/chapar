using Chapar.Core.Abstractions;
using Chapar.Core.Pipeline;
using Chapar.Pipeline.Behaviours;
using Chapar.Pipeline.Dispatcher;
using Microsoft.Extensions.DependencyInjection;

namespace Chapar.Pipeline.Extensions;

public static class ChaparPipelineExtensions
{
    /// <summary>
    /// Adds the Chapar pipeline infrastructure to the service collection by decorating
    /// all registered <see cref="IMessageHandler{T}"/> with the registered <see cref="IPipelineBehavior{T}"/>.
    /// </summary>
    public static IServiceCollection AddChaparPipeline(this IServiceCollection services)
    {
        // 1. Diagnostics – outermost
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(DiagnosticsBehaviour<>));

        // 2. Error Handling – wraps all remaining behaviors
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(ErrorHandlingBehaviour<>));

        // 3. Domain Exception Handling – must wrap Origin Validation
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(DomainExceptionHandlingBehaviour<>));

        // 4. Origin Validation – innermost, runs first
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(OriginValidationBehaviour<>));

        // Pipeline dispatcher decorator
        // Decorate all IMessageHandler<T> registrations with the pipeline dispatcher
        services.TryDecorate(typeof(IMessageHandler<>), typeof(PipelineMessageHandlerDispatcher<>));

        return services;
    }

    /// <summary>
    /// Registers an open‑generic pipeline behaviour that will be applied to all message types.
    /// The behaviour type must be an open generic class that implements <see cref="IPipelineBehavior{T}"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <param name="openGenericBehaviorType">
    /// The open generic type of the behaviour, e.g. <c>typeof(FluentValidationBehaviour&lt;&gt;)</c>.
    /// </param>
    public static IServiceCollection AddChaparPipelineBehavior(this IServiceCollection services,
                                                               Type openGenericBehaviorType)
    {
        if (!openGenericBehaviorType.IsGenericTypeDefinition)
            throw new ArgumentException("The behaviour type must be an open generic type.", nameof(openGenericBehaviorType));

        services.AddScoped(typeof(IPipelineBehavior<>), openGenericBehaviorType);
        return services;
    }
}
