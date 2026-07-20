$apiDir = "api"
$outputFile = "index.md"

$nsFiles = Get-ChildItem -Path $apiDir -Filter "*.yml" |
    Where-Object { $_.Name -ne "toc.yml" } |
    Sort-Object Name

$content = @"
---
title: Chapar - The Persian Courier for .NET Messaging
---

# Chapar

The clean, extensible, and business-friendly messaging abstraction for .NET.

```text
dotnet add package Chapar
dotnet add package Chapar.MassTransit
```

## Why Chapar?

- Zero ceremony: `PublishAsync` and `SendAsync` are all you need.
- Transparent Outbox and Inbox support for reliable delivery and idempotent consumers.
- A configurable pipeline for diagnostics, error handling, validation, and origin checks.
- Standalone usage and Zamin integration.
- MassTransit and RabbitMQ transport support.

## Documentation

- [Complete Guide](docs/guide.md)
- [API Reference](api/toc.yml)
- [GitHub Repository](https://github.com/MiladBhrlo/chapar)

## API Reference

"@

$currentPackage = ""
foreach ($file in $nsFiles) {
    $name = $file.BaseName
    $parts = $name.Split('.')
    $packageName = if ($parts.Length -ge 2) { "$($parts[0]).$($parts[1])" } else { $parts[0] }

    if ($packageName -ne $currentPackage) {
        $currentPackage = $packageName
        $content += "`n### $currentPackage`n`n"
    }

    $content += "- [$name](api/$name.html)`n"
}

$content += @"

## License

MIT
"@

Set-Content -Path $outputFile -Value $content -Encoding UTF8

Write-Host "Generated $outputFile with $($nsFiles.Count) namespaces."
