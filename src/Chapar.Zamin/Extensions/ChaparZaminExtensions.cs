using Chapar.Core.Abstractions;
using Chapar.Core.Messages;
using Chapar.Zamin.Consumer;
using Chapar.Zamin.SendMessageBus;
using Microsoft.Extensions.DependencyInjection;
using Zamin.Extensions.MessageBus.Abstractions;

namespace Chapar.Zamin.Extensions;

public static class ChaparZaminExtensions
{
    /// <summary>
    /// Registers Chapar as the implementation for <see cref="ISendMessageBus"/>
    /// and wires up the <see cref="ChaparMessageConsumer"/> to process incoming parcels.
    /// </summary>
    public static IServiceCollection AddChaparZamin(this IServiceCollection services)
    {
        // Replace the default ISendMessageBus (used by Outbox polling publisher)
        services.AddScoped<ISendMessageBus, ZaminChaparSendMessageBus>();
        
        return services;
    }
}