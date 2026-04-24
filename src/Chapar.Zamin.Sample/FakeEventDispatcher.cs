using Zamin.Core.Contracts.ApplicationServices.Events;
using Zamin.Core.Domain.Events;
// ---------- Fake Event Dispatcher ----------
public class FakeEventDispatcher : IEventDispatcher
{
    public Task PublishDomainEventAsync<TDomainEvent>(TDomainEvent @event) where TDomainEvent : class, IDomainEvent
    {
        Console.WriteLine($"✅ Domain event handled: {@event.GetType().Name}");
        return Task.CompletedTask;
    }
}
