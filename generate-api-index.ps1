# generate-api-index.ps1
$apiDir = "api"
$outputFile = "index.md"

# پیدا کردن تمام فایل‌های YAML مربوط به Namespace (به‌جز toc.yml)
$nsFiles = Get-ChildItem -Path $apiDir -Filter "*.yml" | Where-Object { $_.Name -ne "toc.yml" } | Sort-Object Name

# شروع ساخت فایل index.md
$content = @"
```text
***
title: Chapar – The Persian Courier for .NET Messaging
***

# 🐎 Chapar

### The Clean, Extensible, and Business‑Friendly Messaging Abstraction for .NET

```text
dotnet add package Chapar
dotnet add package Chapar.MassTransit
```

***

## Why Chapar?

- **Zero ceremony** — `PublishAsync` and `SendAsync` are all you need.
- **Transparent Outbox / Inbox** — Add `one` NuGet package and every message is automatically stored in the database before delivery. No code changes.
- **Pipeline** — A chain of configurable behaviours (diagnostics, error handling, origin validation, …) that wrap every handler.
- **Framework agnostic** — Works standalone or on top of **Zamin**.
- **Transport agnostic** — Currently uses **MassTransit v8** (free & community‑supported), with **Wolverine** coming soon.

> Inspired by the ancient Persian courier system – fast, reliable, and invisible to the message sender.

***

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

***

## Explore the Documentation

| Section | Description |
| :--- | :--- |
| [Complete Guide](docs/guide.html) | Walk through every scenario from publish/subscribe to advanced Outbox/Inbox, Pipeline, and Zamin integration. |
| [GitHub Repository](https://github.com/MiladBhrlo/chapar) | Source code, issue tracker, and contribution guidelines. |

***

## API Reference (Generated Automatically)

Welcome to the complete API reference for Chapar.  
Below you can find every namespace documented in the library.

## Documentation

- [Home (README)](README.html)
- [Complete Guide](docs/guide.html)

***

"@

$currentPackage = ""
foreach ($file in $nsFiles) {
    $name = $file.BaseName                     # مثلاً "Chapar.Core.Abstractions"
    $parts = $name.Split('.')
    if ($parts.Length -ge 2) {
        $packageName = "$($parts[0]).$($parts[1])"   # "Chapar.Core"
    } else {
        $packageName = $parts[0]
    }

    # اگر پکیج تغییر کرد، یک هدر <h2> اضافه کن
    if ($packageName -ne $currentPackage) {
        $currentPackage = $packageName
        $content += "`n## $currentPackage`n`n"
    }

    # اضافه کردن لینک به صفحه HTML (مسیر نسبی از ریشه)
    $content += "- [$name](api/$name.html)`n"
}

# بخش ثابت نهایی (Packages, License, etc.)
$content += @"

***

## License

MIT
"@

# نوشتن در فایل
Set-Content -Path $outputFile -Value $content -Encoding UTF8

Write-Host "Generated $outputFile with $($nsFiles.Count) namespaces"