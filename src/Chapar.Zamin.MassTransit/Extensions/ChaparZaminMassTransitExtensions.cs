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
        // 1. جمع‌آوری تمام اسمبلی‌های بارگذاری‌شده برای اسکن Handlerها
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .ToArray();

        var zaminAssembly = typeof(ChaparMessageConsumer).Assembly;
        if (!assemblies.Contains(zaminAssembly))
        {
            assemblies.Append(zaminAssembly);
        }

        // 2. ثبت MassTransit با اسکن خودکار Handlerها (از جمله ChaparMessageConsumer)
        services.AddChaparMassTransit(configure, assemblies.ToArray());

        // 3. ثبت Zamin (ISendMessageBus) بدون وابستگی به MassTransit
        services.AddChaparZamin();

        return services;
    }
}