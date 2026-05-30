# Exceptionless.Extensions.Hosting Client


## When To Use

Use `Exceptionless.Extensions.Hosting` for Generic Host workers, services, queue processors, daemons, and mixed host/web apps. It registers `ExceptionlessClient` in DI and adds a lifetime service that flushes the queue during shutdown.

## Install

```bash
dotnet add package Exceptionless.Extensions.Hosting
dotnet add package Exceptionless.Extensions.Logging
```

## .NET 8+ HostApplicationBuilder Setup

```csharp
using System.Threading;
using System.Threading.Tasks;
using Exceptionless;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.AddExceptionless();
builder.AddExceptionless(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.DefaultData["Worker"] = "billing";
});
builder.UseExceptionless();

builder.Services.AddHostedService<BillingWorker>();

await builder.Build().RunAsync();

public sealed class BillingWorker : BackgroundService {
    private readonly ExceptionlessClient _exceptionless;
    private readonly ILogger<BillingWorker> _logger;

    public BillingWorker(ExceptionlessClient exceptionless, ILogger<BillingWorker> logger) {
        _exceptionless = exceptionless;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) {
        _logger.LogWarning("Billing worker started.");
        _exceptionless.SubmitFeatureUsage("BillingWorker.Started");
        return Task.CompletedTask;
    }
}
```

## Older IHostBuilder Setup

Use `.UseExceptionless()` on `IHostBuilder` and `services.AddExceptionless(...)` inside `ConfigureServices`.

```text
Host.CreateDefaultBuilder(args)
  .ConfigureLogging(builder => builder.AddExceptionless())
  .UseExceptionless()
  .ConfigureServices(services => {
      services.AddExceptionless(c => c.ApiKey = "VALID_API_KEY_12345");
  })
```

## Worker Usage

Inject `ExceptionlessClient` anywhere the DI container is available. Use it for feature usage, handled exceptions, custom events, and manual queue flushing in unusual shutdown flows.

## Shutdown Behavior

The hosting package registers `ExceptionlessLifetimeService` as both `IHostedService` and `IHostedLifecycleService`. It calls `ProcessQueueAsync()` during host stop. Avoid duplicate manual shutdown flushing unless you know the process exits before the host stop lifecycle.

## Best Practices

- Pair with `Exceptionless.Extensions.Logging` when the app already uses `ILogger`.
- Use `builder.AddExceptionless()` without an API key when `appsettings.json` or environment variables supply it.
- Put queue durability settings in configuration for services that may stop abruptly.
- For high-volume workers, use remote log levels and event type/source settings to reduce noise.
