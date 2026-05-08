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
    private readonly IChaparBus `bus`;

    public RegistrationService(IChaparBus bus) => `bus` = bus;

    public async Task RegisterAsync(User user)
    {
        // ... save user ...
        await `bus`.PublishAsync(new UserRegistered(user.Id, user.Email));
    }
}
```

### 1.5 Handling Messages

```csharp
public class UserRegisteredHandler : IMessageHandler<UserRegistered>
{
    public Task HandleAsync(UserRegistered message, CancellationToken ct)
    {
        Console.WriteLine(`$"User {message.Email} registered."`);
        return Task.CompletedTask;
    }
}
```

``No manual registration needed`` – Chapar scans all assemblies automatically.

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
await `bus`.SendAsync(new SendWelcomeEmail(user.Id), "email-service");
```

---

## 3. Outbox Pattern (Guaranteed Delivery)

### 3.1 Install

```bash
dotnet add package Chapar.Outbox.EntityFrameworkCore
```

### 3.2 Configure

```csharp
services.AddChaparOutboxEntityFramework(); // Enables outbox for `all` messages

// In your DbContext:
protected override void OnModelCreating(ModelBuilder builder)
{
    builder.ConfigureChaparOutbox();
}
```

After this, ``every`` `PublishAsync` / `SendAsync` call is stored in the outbox table
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
services.AddChaparPipeline(); // Adds diagnostics, error handling, domain exception handling, and origin validation
```

### 5.3 Custom Behaviours

Create a class that implements `IPipelineBehavior<TMessage>`:

```csharp
public class LoggingBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : class, IMessage
{
    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken ct)
    {
        Console.WriteLine(`$"Before {typeof(TMessage).Name}"`);
        await next();
        Console.WriteLine(`$"After {typeof(TMessage).Name}"`);
    }
}
```

Register it:

```csharp
services.AddChaparPipelineBehavior(typeof(LoggingBehaviour<>));
```

---

## 6. Headers, Multi‑Tenancy, and Security

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
await `bus`.PublishAsync(new OrderPlaced(orderId), headers);
```

### 6.3 Header Access in Pipeline

Chapar provides an `IMessageContextAccessor` service that allows pipeline behaviors
to both ``read`` and ``write`` message headers.

```csharp
public class TenantPropagationBehaviour<TMessage> : IPipelineBehavior<TMessage>
    where TMessage : IMessage
{
    private readonly IMessageContextAccessor `accessor`;

    public TenantPropagationBehaviour(IMessageContextAccessor accessor) => `accessor` = accessor;

    public async Task HandleAsync(TMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        `accessor`.Headers ??= new Dictionary<string, object?>();
        `accessor`.Headers["TenantId"] = "tenant-123";
        await next();
    }
}
```

### 6.4 Origin Validation

Apply `[AllowedOrigin("OrderService")]` on your handler, and Chapar automatically validates the `Origin` header.
This behavior is enabled by default when you call `AddChaparPipeline()`.

```csharp
[AllowedOrigin("OrderService")]
public class FinalizeInvoiceHandler : IMessageHandler<OrderPlaced> { ... }
```

Set the origin header when publishing:

```csharp
await `bus`.PublishAsync(new OrderPlaced(), new Dictionary<string, object>
{
    ["Origin"] = "OrderService"
});
```

### 6.5 HeaderSanitizer

Use `HeaderSanitizer.Sanitize()` before logging or persisting headers.
This helper redacts sensitive keys (like `Authorization`, `Token`)
and strips dangerous characters (CRLF, NULL) to prevent injection attacks.

```csharp
var safeHeaders = HeaderSanitizer.Sanitize(headers);
logger.LogInformation("Headers: {@Headers}", safeHeaders);
```

---

## 7. Zamin Integration

### 7.1 Installation

```bash
dotnet add package Chapar.Zamin.MassTransit
dotnet add package Chapar.Zamin.Outbox   # For outbox on Zamin's native tables
```

### 7.2 Setup

```csharp
services.AddChaparZaminMassTransit(opt => opt.Host = "localhost");
services.AddChaparZaminOutbox();   // Uses Zamin's Outbox ``and`` Inbox tables
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
| `DefaultHeaders` | `{}` | Headers added to every message |

### `ResilienceOptions`

| Property | Default | Description |
| :--- | :--- | :--- |
| `RetryCount` | `3` | Immediate retries |
| `RetryInterval` | `00:00:05` | Interval between retries |
| `CircuitBreakerEnabled` | `true` | Enable / disable CB |
| `CircuitBreakerFailureThreshold` | `20` | `%` failure to trip |
| `CircuitBreakerResetInterval` | `00:01:00` | Reset interval |

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
- Origin Validation
- Zamin framework integration

---

## 10. Automated Table Cleanup

Chapar automatically cleans up old processed records from Inbox, Outbox, and Zamin Outbox tables.
The default retention period is 7 days, and the cleanup runs every hour.
You can customize or disable this behavior.

### Default Usage

```csharp
// Customize retention to 30 days
services.AddChaparInboxEntityFramework(
    configureCleanup: opt => opt.RetentionPeriod = TimeSpan.FromDays(30));

// Disable cleanup entirely
services.AddChaparOutboxEntityFramework(
    configureCleanup: opt => opt.Enabled = false);
```

### Custom Cleanup Store

For advanced scenarios, you can register a custom cleanup store that implements `ICleanupStore`:

```csharp
services.AddInboxCleanup<MyCustomStore>(opt => opt.RetentionPeriod = TimeSpan.FromHours(12));
```

### Zamin Inbox Cleanup

Chapar does ``not`` provide a built‑in cleanup job for Zamin Inbox.
The underlying `IMessageInboxItemRepository` may use different storage technologies,
so a single cleanup strategy cannot be assumed.

If you need automatic cleanup, you can implement `ICleanupStore` yourself
and register it with `AddInboxCleanup` (which adds a background hosted service):

```csharp
public class ZaminInboxCleanupStore : ICleanupStore
{
    public Task<int> DeleteProcessedAsync(DateTime olderThan, CancellationToken ct)
    {
        // Implement your cleanup logic here
    }
}

services.AddInboxCleanup<ZaminInboxCleanupStore>(opt => opt.RetentionPeriod = TimeSpan.FromDays(14));
```

---

## 11. Monitoring & Health Checks

Chapar provides built‑in metrics and health checks to monitor the message bus, inbox, and outbox processing.

### Health Check

You can register a health check that reports the status of the MassTransit bus and all its endpoints:

```csharp
builder.Services.AddHealthChecks().AddChaparMassTransitHealthCheck();
```

This health check is available at the standard `/healthz` endpoint when configured.

### Metrics

Chapar records the following counters for inbox and outbox message processing.

#### Inbox Metrics

- `chapar.inbox.processed` – Successfully processed incoming messages
- `chapar.inbox.duplicate` – Duplicate incoming messages skipped
- `chapar.inbox.failed` – Incoming messages that failed processing

#### Outbox Metrics

- `chapar.outbox.published` – Outbox messages successfully published to the broker
- `chapar.outbox.failed` – Outbox messages that failed to publish
- `chapar.outbox.pending` – Current number of outbox messages waiting to be published (observable gauge)

Metrics are exposed using `System.Diagnostics.Metrics` and can be collected by any OpenTelemetry‑compatible tool.

### Distributed Tracing

The `DiagnosticsBehaviour` in the Chapar pipeline automatically creates an `Activity` for each handled message,
with tags for message type and messaging kind. This integrates with OpenTelemetry tracing.

No additional configuration is required – it works out of the box when the pipeline is enabled.