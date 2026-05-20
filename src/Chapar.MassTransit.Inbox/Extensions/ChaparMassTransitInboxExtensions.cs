using Chapar.Core.Abstractions;
using Chapar.Core.Inbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chapar.MassTransit.Inbox.Extensions;

/// <summary>
/// Extension methods for configuring Chapar to use the native MassTransit inbox
/// instead of Chapar's custom inbox filter.
/// </summary>
public static class ChaparMassTransitInboxExtensions
{
    /// <summary>
    /// Removes Chapar's custom inbox filter and delegates idempotency to MassTransit's
    /// built‑in inbox (<c>InboxState</c> table), which is automatically enabled when the
    /// MassTransit outbox is configured on the consumer side.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/>.</param>
    /// <returns>The same service collection for chaining.</returns>
    /// <remarks>
    /// This method is only needed if you want to use MassTransit's inbox without also enabling
    /// the MassTransit outbox. If both are desired, a single call to
    /// <c>AddChaparMassTransitOutbox&lt;TDbContext&gt;()</c> is sufficient, as MassTransit's
    /// consumer‑side outbox inherently provides inbox guarantees.
    /// </remarks>
    public static IServiceCollection AddChaparMassTransitInbox(this IServiceCollection services)
    {
        // Remove Chapar‑specific inbox filter and store
        services.RemoveAll<IConsumeFilter>();
        services.RemoveAll<IInboxStore>();

        // MassTransit will now handle idempotency via InboxState
        return services;
    }
}
