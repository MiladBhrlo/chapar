# 🐎 Chapar

[![Ask DeepWiki](https://devin.ai/assets/askdeepwiki.png)](https://deepwiki.com/MiladBhrlo/Chapar)

**Chapar** is a clean, extensible, and business‑friendly messaging abstraction for .NET.
It hides the complexity of RabbitMQ and MassTransit behind a minimal API, while providing
out‑of‑the‑box support for the **Outbox**, **Inbox**, and **Pipeline** patterns.

> Inspired by the ancient Persian courier system – fast, reliable, and invisible to the message sender.

## Why Chapar?

- **Zero ceremony**: `PublishAsync` and `SendAsync` are all you need.
- **Transparent Outbox / Inbox**: Add *one* NuGet package and every message is automatically stored in the database before delivery. No code changes.
- **Pipeline**: a chain of configurable behaviours (logging, error handling, validation, …) that wrap every handler.
- **Framework agnostic**: works standalone or on top of **Zamin**.
- **Transport agnostic**: currently uses **MassTransit v8** (free & community‑supported), with **Wolverine** coming soon.

## Packages

| Package | Description |
| :--- | :--- |
| `Chapar` (`Chapar.Core`) | Core abstractions: `IChaparBus`, `IMessageHandler<T>`, `Outbox`/`Inbox` contracts |
| `Chapar.MassTransit` | MassTransit + RabbitMQ implementation |
| `Chapar.Pipeline` | Extensible message handling pipeline |
| `Chapar.Outbox.EntityFrameworkCore` | EF Core‑based Outbox (transparent decorator) |
| `Chapar.Inbox.EntityFrameworkCore` | EF Core‑based Inbox (idempotency filter) |
| `Chapar.Zamin` | Bridges Chapar with the Zamin framework |
| `Chapar.Zamin.MassTransit` | One‑line setup for Chapar + Zamin + MassTransit |
| `Chapar.Zamin.Outbox` | Outbox/Inbox stores backed by Zamin's native tables |

## Quick start (standalone)

```bash
dotnet add package Chapar
dotnet add package Chapar.MassTransit
```

```csharp
services.AddChaparMassTransit(opt => opt.Host = "localhost");

// Define a message
public record OrderPlaced(Guid OrderId) : IEvent;

// Publish
var bus = provider.GetRequiredService<IChaparBus>();
await bus.PublishAsync(new OrderPlaced(Guid.NewGuid()));

// Handle
public class OrderPlacedHandler : IMessageHandler<OrderPlaced>
{
    public Task HandleAsync(OrderPlaced message, CancellationToken ct)
    {
        Console.WriteLine($"Order {message.OrderId} received.");
        return Task.CompletedTask;
    }
}
```

## EF Core Outbox

`Chapar.Outbox.EntityFrameworkCore` stores outgoing messages in the application's
EF Core context before they are delivered by the background publisher.

By default, outbox messages are staged and committed with the caller's unit of work:

```csharp
services.AddChaparOutboxEntityFramework(options =>
{
    options.DefaultSaveMode = OutboxSaveMode.Transactional;
});

await bus.PublishAsync(new OrderPlaced(orderId));
await dbContext.SaveChangesAsync(ct);
```

This keeps the business data and the outbox row in the same database transaction.

For messages that should be persisted after the business transaction has already
completed, use the EF outbox extension overloads:

```csharp
using Chapar.Outbox.EntityFrameworkCore;
using Chapar.Outbox.EntityFrameworkCore.Extensions;

await dbContext.SaveChangesAsync(ct);

await bus.PublishAsync(
    new WelcomeSmsRequested(userId),
    OutboxSaveMode.Immediate,
    cancellationToken: ct);
```

`Immediate` performs a separate `SaveChangesAsync` through the outbox store. Use it
after committing any business changes that must not be part of the outbox transaction.

## Documentation

- [Complete Guide](docs/guide.md) – from simple publish/subscribe to advanced Outbox/Inbox, Pipeline, and Zamin integration.
- [API Reference](https://miladbhrlo.github.io/chapar/)

## Roadmap / Backlog

We welcome contributions! If you are interested in any of the items below, feel free to open an issue or send a pull request.

### ✅ Completed

| Feature / Package | Description |
| :--- | :--- |
| `Chapar.Core` | Core abstractions and contracts |
| `Chapar.MassTransit` | MassTransit v8 integration |
| `Chapar.Pipeline` | Pipeline behaviour infrastructure (diagnostics, error handling, etc.) |
| `Chapar.Outbox.EntityFrameworkCore` | Outbox store backed by EF Core |
| `Chapar.Inbox.EntityFrameworkCore` | Inbox store backed by EF Core |
| `Chapar.Zamin` | Zamin framework integration (replaces `ISendMessageBus`) |
| `Chapar.Zamin.MassTransit` | One‑line setup for Chapar + Zamin + MassTransit |
| `Chapar.Zamin.Outbox` | Outbox/Inbox stores backed by Zamin's native tables |

### 🚧 In Progress (by core team)

| Feature / Package | Description |
| :--- | :--- |
| `Chapar.RoutingSlip` | Content‑based routing (Courier / Routing Slip) |
| `Chapar.Saga` | Saga / Process Manager (state machine) support |

### 💡 Backlog (up for grabs – contributions welcome!)

| Feature / Package | Description |
| :--- | :--- |
| `Chapar.Wolverine` | Wolverine transport adapter |
| `Chapar.Outbox.Dapper` | Outbox store using Dapper |
| `Chapar.Inbox.Dapper` | Inbox store using Dapper |
| `Chapar.Outbox.InMemory` | In‑memory Outbox (for testing / lightweight scenarios) |
| `Chapar.Inbox.InMemory` | In‑memory Inbox (for testing) |
| `Chapar.Outbox.Redis` | Outbox store backed by Redis |
| `Chapar.Inbox.Redis` | Inbox store backed by Redis |
| `Chapar.RequestResponse` | Stronger abstractions for request/response patterns |
| `Chapar.FluentValidation` | Pre‑built pipeline behaviour for FluentValidation |
| `Chapar.Authorization` | Security behaviour for header/token checks |
| `Chapar.Outbox.MongoDB` | Outbox store for MongoDB |
| `Chapar.Inbox.MongoDB` | Inbox store for MongoDB |
| Management / Observability | Dashboard, dead‑letter browser, metrics |

## License

MIT
