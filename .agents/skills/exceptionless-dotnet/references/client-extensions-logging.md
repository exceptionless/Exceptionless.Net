# Exceptionless.Extensions.Logging Provider


## When To Use

Use `Exceptionless.Extensions.Logging` when the app uses `Microsoft.Extensions.Logging` and should send selected `ILogger` entries to Exceptionless. It is a provider, not a full app integration by itself; pair it with a configured Exceptionless client through ASP.NET Core or hosting whenever possible.

## Install

```bash
dotnet add package Exceptionless.Extensions.Logging
```

## Setup With Configured Client

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddExceptionless(c => c.ApiKey = "VALID_API_KEY_12345");
builder.Logging.AddExceptionless();
builder.UseExceptionless();
builder.Services.AddHostedService<LoggingWorker>();

await builder.Build().RunAsync();

public sealed class LoggingWorker : BackgroundService {
    private readonly ILogger<LoggingWorker> _logger;

    public LoggingWorker(ILogger<LoggingWorker> logger) {
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        using (_logger.BeginScope("startup")) {
            _logger.LogError(new InvalidOperationException("Example"),
                "Unable to load order {OrderId} for tenant {TenantId}.",
                42,
                "tenant-a");
        }

        return Task.CompletedTask;
    }
}
```

## Provider Overloads

Available overloads:

- `AddExceptionless()` uses the DI `ExceptionlessClient` or `ExceptionlessClient.Default`.
- `AddExceptionless(ExceptionlessClient client)` uses a supplied client.
- `AddExceptionless(string apiKey, string serverUrl = null)` creates/configures a provider client and uses in-memory storage.
- `AddExceptionless(Action<ExceptionlessConfiguration> configure)` configures `ExceptionlessClient.Default` for the provider.

## Structured Logging Behavior

The provider:

- Uses the logger category as event source.
- Converts exceptions to error events and non-exception logs to log events.
- Adds non-`{OriginalFormat}` structured state values as event properties.
- Adds `EventId` when non-zero.
- Links nested logging scopes with event references.

## Log Level Mapping

| Microsoft | Exceptionless |
| --- | --- |
| `Trace` | `Trace` |
| `Debug` | `Debug` |
| `Information` | `Info` |
| `Warning` | `Warn` |
| `Error` | `Error` |
| `Critical` | `Fatal` |
| `None` | `Off` |

The provider sets `@@log:*` to `Trace` if no remote/default log-level setting exists, so Microsoft logging configuration controls initial filtering. Remote Exceptionless settings can still override by source.

## Best Practices

- Configure Microsoft logging rules to avoid creating avoidable log volume.
- Use Exceptionless remote log level settings for operational changes without redeploying.
- Avoid logging secrets in structured properties; data exclusions are a backstop, not a license to log sensitive data.
- For logging-only high-throughput clients, use in-memory storage and document the loss-of-unsent-events tradeoff.
