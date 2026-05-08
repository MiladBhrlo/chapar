---

_layout: landing
title: Chapar – The Persian Courier for .NET Messaging

---

# 🐎 Chapar

### The Clean, Extensible, and Business‑Friendly Messaging Abstraction for .NET

```text
dotnet add package Chapar
dotnet add package Chapar.MassTransit
```

---

## Why Chapar?

- **Zero ceremony** — `PublishAsync` and `SendAsync` are all you need.
- **Transparent Outbox / Inbox** — Add `one` NuGet package and every message is automatically stored in the database before delivery. No code changes.
- **Pipeline** — A chain of configurable behaviours (diagnostics, error handling, origin validation, …) that wrap every handler.
- **Framework agnostic** — Works standalone or on top of **Zamin**.
- **Transport agnostic** — Currently uses **MassTransit v8** (free & community‑supported), with **Wolverine** coming soon.

> Inspired by the ancient Persian courier system – fast, reliable, and invisible to the message sender.

---

## Quick Start

```csharp
// 1. Define a message
public record UserRegistered(Guid UserId, string Email) : IEvent;

// 2. Publish
var bus = provider.GetRequiredService`IChaparBus`();
await bus.PublishAsync(new UserRegistered(Guid.NewGuid(), "user@example.com"));

// 3. Handle
public class UserRegisteredHandler : IMessageHandler`UserRegistered`
{
    public Task HandleAsync(UserRegistered message, CancellationToken ct)
    {
        Console.WriteLine($"User {message.Email} registered.");
        return Task.CompletedTask;
    }
}
```

---

## Explore the Documentation

| Section | Description |
| :--- | :--- |
| [Complete Guide](docs/guide.md) | Walk through every scenario from publish/subscribe to advanced Outbox/Inbox, Pipeline, and Zamin integration. |
| [API Reference](api/) | Browse the full API surface with every class, interface, and method documented. |
| [GitHub Repository](https://github.com/MiladBhrlo/chapar) | Source code, issue tracker, and contribution guidelines. |

---

## Packages

| Package | Description |
| :--- | :--- |
| `Chapar` (`Chapar.Core`) | Core abstractions: `IChaparBus`, `IMessageHandler`T``, `Outbox`/`Inbox` contracts |
| `Chapar.MassTransit` | MassTransit + RabbitMQ implementation |
| `Chapar.Pipeline` | Extensible message handling pipeline |
| `Chapar.Outbox.EntityFrameworkCore` | EF Core‑based Outbox (transparent decorator) |
| `Chapar.Inbox.EntityFrameworkCore` | EF Core‑based Inbox (idempotency filter) |
| `Chapar.Zamin` | Bridges Chapar with the Zamin framework |
| `Chapar.Zamin.MassTransit` | One‑line setup for Chapar + Zamin + MassTransit |
| `Chapar.Zamin.Outbox` | Outbox/Inbox stores backed by Zamin's native tables |

---

## License

MIT
