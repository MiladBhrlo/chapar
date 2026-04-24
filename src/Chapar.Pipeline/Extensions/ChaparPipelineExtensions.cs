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
        // Register all default behaviours
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(DiagnosticsBehaviour<>)); // 1
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(ErrorHandlingBehaviour<>)); // 2
        services.AddScoped(typeof(IPipelineBehavior<>), typeof(DomainExceptionHandlingBehaviour<>)); // 3

        // Decorate all IMessageHandler<T> registrations with the pipeline dispatcher
        services.TryDecorate(typeof(IMessageHandler<>), typeof(PipelineMessageHandlerDispatcher<>)); // 4

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