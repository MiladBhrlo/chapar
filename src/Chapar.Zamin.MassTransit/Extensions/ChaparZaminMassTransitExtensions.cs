using Chapar.MassTransit.Extensions;
using Chapar.MassTransit.Options;
using Chapar.Zamin.Consumer;
using Chapar.Zamin.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Chapar.Zamin.MassTransit.Extensions;

public static class ChaparZaminMassTransitExtensions
{
    /// <summary>
    /// Registers all Chapar + Zamin services backed by MassTransit.
    /// Automatically scans loaded assemblies for message handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure MassTransit/RabbitMQ options.</param>
    public static IServiceCollection AddChaparZaminMassTransit(this IServiceCollection services,
                                                               Action<ChaparMassTransitOptions> configure)
    {
        // Collect all loaded assemblies to scan for handlers
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToArray();

        var zaminAssembly = typeof(ChaparMessageConsumer).Assembly;
        if (!assemblies.Contains(zaminAssembly))
        {
            assemblies.Append(zaminAssembly);
        }

        // Register MassTransit with automatic handler scanning (including ChaparMessageConsumer)
        services.AddChaparMassTransit(configure, assemblies.ToArray());

        // Register Zamin (ISendMessageBus) without MassTransit dependency
        services.AddChaparZamin();

        return services;
    }
}
