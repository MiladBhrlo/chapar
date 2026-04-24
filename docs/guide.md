# Chapar – Complete Guide

This guide walks through every supported scenario, from the simplest possible
usage to the most advanced configurations.

---

## 1. Getting Started

### 1.1 Installation

```bash
dotnet add package Chapar
dotnet add package Chapar.MassTransit
```

### 1.2 Basic Setup

```csharp
// Program.cs
using Chapar.Core.Abstractions;
using Chapar.MassTransit.Extensions;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((ctx, services) =>
{
    services.AddChaparMassTransit(opt =>
    {
        opt.Host = "localhost";
        opt.Username = "guest";
        opt.Password = "guest";
    });
});
```

### 1.3 Defining Messages

```csharp
// Events (broadcast)
public record UserRegistered(Guid UserId, string Email) : IEvent;

// Commands (point‑to‑point)
public record SendWelcomeEmail(Guid UserId) : ICommand;
```

### 1.4 Publishing / Sending

```csharp
public class RegistrationService
{
    private readonly IChaparBus _bus;

    public RegistrationService(IChaparBus bus) => _bus = bus;

    public async Task RegisterAsync(User user)
    {
        // ... save user ...
        await _bus.PublishAsync(new UserRegistered(user.Id, user.Email));
    }
}
```

### 1.5 Handling Messages

```csharp
public class UserRegisteredHandler : IMessageHandler<UserRegistered>
{
    public Task HandleAsync(UserRegistered message, CancellationToken ct)
    {
        Console.WriteLine($"User {message.Email} registered.");
        return Task.CompletedTask;
    }
}
```

**No manual registration needed** – Chapar scans all assemblies automatically.

---

## 2. Sending Commands (Point‑to‑Point)

### 2.1 The `[QueueName]` Attribute

```csharp
[QueueName("email-service")]
public class SendWelcomeEmailHandler : IMessageHandler<SendWelcomeEmail>
{
    public Task HandleAsync(SendWelcomeEmail message, CancellationToken ct) => ...;
}
```

### 2.2 Sending

```csharp
await _bus.SendAsync(new SendWelcomeEmail(user.Id), "email-service");
```

---

## 3. Outbox Pattern (Guaranteed Delivery)

### 3.1 Install

```bash
dotnet add package Chapar.Outbox.EntityFrameworkCore
```

### 3.2 Configure

```csharp
services.AddChaparOutboxEntityFramework(); // Enables outbox for *all* messages

// In your DbContext:
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.ConfigureChaparOutbox();
}
```

After this, **every** `PublishAsync` / `SendAsync` call is stored in the outbox table
and dispatched later by a background service.

### 3.3 Aggregate‑Root Integration (optional)

If your entities implement `IAggregateRoot`, the `OutboxInterceptor` automatically
extracts events during `SaveChangesAsync` and persists them.

```csharp
public class Order : AggregateRoot
{
    public void Place()
    {
        AddDomainEvent(new OrderPlaced(Id));
    }
}
```

Use `ChaparOutboxOptions` to decide which events are persisted.

```csharp
services.Configure<ChaparOutboxOptions>(opt =>
{
    opt.PublishDomainEvents = true;
    opt.PublishIntegrationEvents = true;
});
```

---

## 4. Inbox Pattern (Idempotency)

### 4.1 Install

```bash
dotnet add package Chapar.Inbox.EntityFrameworkCore
```

### 4.2 Configure

```csharp
services.AddChaparInboxEntityFramework(); // Automatically filters duplicates

// In your DbContext:
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.ConfigureChaparInbox();
}
```

No handler changes needed – the `InboxConsumeFilter` runs transparently.

---

## 5. Pipeline (Behaviours)

### 5.1 Install

```bash
dotnet add package Chapar.Pipeline
```

### 5.2 Enable

```csharp
services.AddChaparPipeline(); // Adds diagnostics, error handling, domain exception handling
```

### 5.3 Custom Behaviours

Create a class that implements `IPipelineBehavior<TMessage>`:

```csharp
public class LoggingBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : class, IMessage
{
    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken ct)
    {
        Console.WriteLine($"Before {typeof(TMessage).Name}");
        await next();
        Console.WriteLine($"After {typeof(TMessage).Name}");
    }
}
```

Register it:

```csharp
services.AddChaparPipelineBehavior(typeof(LoggingBehaviour<>));
```

Pre‑built behaviours (just add the package):
- **FluentValidation** (community)
- **Origin / Authorization** (you can write your own)

---

## 6. Headers & Multi‑Tenancy

### 6.1 Default Headers

```csharp
services.AddChaparMassTransit(opt =>
{
    opt.DefaultHeaders["X-Tenant"] = "tenant-A";
});
```

All outgoing messages will carry this header.

### 6.2 Per‑Message Headers

```csharp
var headers = new Dictionary<string, object> { ["Priority"] = "High" };
await _bus.PublishAsync(new OrderPlaced(orderId), headers);
```

---

## 7. [Zamin](https://github.com/oroumand/Zamin) Integration

### 7.1 Installation

```bash
dotnet add package Chapar.Zamin.MassTransit
dotnet add package Chapar.Zamin.Outbox   # For outbox on Zamin's native tables
```

### 7.2 Setup

```csharp
services.AddChaparZaminMassTransit(opt => opt.Host = "localhost");
services.AddChaparZaminOutbox();   // Uses Zamin's Outbox & Inbox tables
```

Now every `ISendMessageBus.Send(parcel)` call goes through Chapar.
Incoming messages are received by `ChaparMessageConsumer` and dispatched
via Zamin's `IEventDispatcher`.

---

## 8. Configuration Reference

### `ChaparMassTransitOptions`

| Property | Default | Description |
| :--- | :--- | :--- |
| `Host` | `localhost` | RabbitMQ host |
| `Username` | `guest` | Login username |
| `Password` | `guest` | Login password |
| `VirtualHost` | `/` | RabbitMQ vhost |
| `RetryCount` | `3` | Immediate retries |
| `RetryInterval` | `00:00:05` | Interval between retries |
| `CircuitBreakerEnabled` | `true` | Enable / disable CB |
| `CircuitBreakerFailureThreshold` | `20` | % failure to trip |
| `CircuitBreakerResetInterval` | `00:01:00` | Reset interval |
| `DefaultHeaders` | `{}` | Headers added to every message |

---

## 9. Supported Patterns Summary

- Publish / Subscribe (fan‑out)
- Point‑to‑Point commands (`[QueueName]`)
- Request / Response (planned)
- Schedule / Delayed messages (planned)
- Outbox (Guaranteed delivery)
- Inbox (Idempotency)
- Pipeline (Behaviours)
- Multi‑tenancy (Headers)
- Zamin framework integration