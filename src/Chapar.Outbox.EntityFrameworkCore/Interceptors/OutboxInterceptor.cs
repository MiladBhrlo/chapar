using Chapar.Core.Abstractions;
using Chapar.Core.Outbox;
using Chapar.Outbox.EntityFrameworkCore.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Chapar.Outbox.EntityFrameworkCore.Interceptors;

/// <summary>
/// An EF Core <see cref="SaveChangesInterceptor"/> that, before saving changes,
/// extracts domain events from aggregate roots and persists the configured subset
/// as <see cref="OutboxMessage"/> records.
/// </summary>
public sealed class OutboxInterceptor : SaveChangesInterceptor
{
    private readonly IOutboxStore _outboxStore;
    private readonly ChaparOutboxOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboxInterceptor"/> class.
    /// </summary>
    /// <param name="outboxStore">The store responsible for persisting outbox messages.</param>
    /// <param name="options">The outbox options.</param>
    public OutboxInterceptor(IOutboxStore outboxStore, IOptions<ChaparOutboxOptions> options)
    {
        _outboxStore = outboxStore ?? throw new ArgumentNullException(nameof(outboxStore));
        _options = options?.Value ?? new ChaparOutboxOptions();
    }

    /// <inheritdoc />
    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
                                                                                InterceptionResult<int> result,
                                                                                CancellationToken cancellationToken = default)
    {
        var dbContext = eventData.Context;
        if (dbContext is null)
            return await base.SavingChangesAsync(eventData, result, cancellationToken);

        // Find all aggregate roots that have pending domain events.
        var aggregates = dbContext.ChangeTracker
            .Entries<IAggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Count != 0);

        foreach (var entry in aggregates)
        {
            // Filter events according to options.
            var eventsToPublish = entry.Entity.DomainEvents
                .Where(e => (e is IIntegrationEvent && _options.PublishIntegrationEvents)
                         || (e is IDomainEvent && _options.PublishDomainEvents))
                .ToList();

            foreach (var domainEvent in eventsToPublish)
            {
                var messageTypeName = domainEvent.GetType().AssemblyQualifiedName
                                      ?? domainEvent.GetType().FullName
                                      ?? domainEvent.GetType().Name;

                var outboxMessage = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    MessageType = messageTypeName,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType()),
                    OccurredOn = DateTime.UtcNow
                };

                await _outboxStore.SaveAsync(outboxMessage, cancellationToken);
            }

            // Clear all domain events from the aggregate to prevent double processing.
            entry.Entity.ClearDomainEvents();
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}