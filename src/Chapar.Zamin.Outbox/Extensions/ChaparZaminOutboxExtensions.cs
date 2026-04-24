using Chapar.Core.Inbox;
using Chapar.Core.Outbox;
using Chapar.Zamin.Outbox.Inbox;
using Chapar.Zamin.Outbox.Outbox;
using Microsoft.Extensions.DependencyInjection;

namespace Chapar.Zamin.Outbox.Extensions;

public static class ChaparZaminOutboxExtensions
{
    /// <summary>
    /// Registers Zamin‑backed implementations for <see cref="IOutboxStore"/> and <see cref="IInboxStore"/>.
    /// </summary>
    public static IServiceCollection AddChaparZaminOutbox(this IServiceCollection services)
    {
        services.AddScoped<IOutboxStore, ZaminOutboxStore>();
        services.AddScoped<IInboxStore, ZaminInboxStore>();
        return services;
    }
}