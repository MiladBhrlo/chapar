# 🐎 Chapar

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

## Documentation

- [Complete Guide](docs/guide.md) – from simple publish/subscribe to advanced Outbox/Inbox, Pipeline, and Zamin integration.
- [API Reference](https://github.com/MiladBhrlo/chapar/wiki) (coming soon)

## License

MIT