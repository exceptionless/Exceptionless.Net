# Exceptionless Core Client For Console Apps And Services


## When To Use `Exceptionless`

Use the root `Exceptionless` package for console apps, CLI tools, simple non-visual services, Blazor WebAssembly, library-level clients, and serverless functions that do not use the ASP.NET Core package. If the app is ASP.NET Core, Generic Host, WPF, Windows Forms, Web API, MVC, NLog, or log4net, prefer the platform package.

## Install

```bash
dotnet add package Exceptionless
```

## Minimal Setup

Call `Startup` early. It reads configuration, applies saved server settings, wires unhandled exception handlers, registers process-exit queue flushing, and enables default trace log entries.

```csharp
using System;
using Exceptionless;
using Exceptionless.Logging;

ExceptionlessClient.Default.Configuration.UseFolderStorage("exceptionless-queue");
ExceptionlessClient.Default.Configuration.SetVersion("1.2.3");
ExceptionlessClient.Default.Configuration.UseFileLogger("exceptionless-client.log", LogLevel.Info);
ExceptionlessClient.Default.Startup("VALID_API_KEY_12345");

try {
    throw new InvalidOperationException("Console example.");
} catch (Exception ex) {
    ex.ToExceptionless()
        .SetReferenceId(Guid.NewGuid().ToString("N"))
        .AddTags("console", "handled")
        .Submit();
}

ExceptionlessClient.Default.SubmitLog("Console job completed.", LogLevel.Info);

await ExceptionlessClient.Default.ProcessQueueAsync();
```

Use a constructed client when tests, multiple isolated clients, or serverless invocation boundaries make the static default awkward.

```csharp
using Exceptionless;

var client = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.DefaultData["Service"] = "billing-job";
    c.DefaultTags.Add("console");
    c.IncludePrivateInformation = false;
});

client.SubmitFeatureUsage("BillingJob.Started");
await client.ProcessQueueAsync();
```

## Short-Lived Processes

The client queues events asynchronously. For CLI tools and jobs that exit quickly, flush before exit.

```csharp
using System;
using Exceptionless;

ExceptionlessClient.Default.Startup("VALID_API_KEY_12345");

try {
    throw new InvalidOperationException("Example.");
} catch (Exception ex) {
    ex.ToExceptionless().Submit();
}

await ExceptionlessClient.Default.ShutdownAsync();
```

## Blazor WebAssembly

Blazor WebAssembly uses the root client in the browser. It does not get ASP.NET Core server middleware, so submit handled errors explicitly or from app-level error handling.

```csharp
using System;
using Exceptionless;

ExceptionlessClient.Default.Configuration.ServerUrl = "https://collector.exceptionless.io";
ExceptionlessClient.Default.Startup("VALID_API_KEY_12345");

try {
    throw new InvalidOperationException("Blazor WebAssembly handled error.");
} catch (Exception ex) {
    ex.ToExceptionless()
        .SetProperty("Component", "Counter")
        .Submit();
}
```

## Serverless Functions

Flush inside the invocation so queued events are not lost when the runtime freezes or exits.

```csharp
using System;
using Exceptionless;

var client = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.ReadFromEnvironmentalVariables();
});

await using var flush = new ProcessQueueScope(client);

try {
    throw new InvalidOperationException("Serverless handled error.");
} catch (Exception ex) {
    ex.ToExceptionless(client).Submit();
}

public sealed class ProcessQueueScope : IAsyncDisposable {
    private readonly ExceptionlessClient _client;

    public ProcessQueueScope(ExceptionlessClient client) {
        _client = client;
    }

    public async ValueTask DisposeAsync() {
        await _client.ProcessQueueAsync();
    }
}
```

## Best Practices

- Put API keys in configuration or secret storage, not source.
- Configure API key/server URL/storage/logger before first event submission.
- Use folder storage for durable short-lived apps; use in-memory storage only when losing unsent events is acceptable.
- Explicitly set `IncludePrivateInformation` and data exclusions for production.
- Use `SetVersion`, `DefaultTags`, and `DefaultData` for release, environment, service, region, and tenant dimensions that apply to all events.
- Flush with `ProcessQueueAsync` or `ShutdownAsync` before exit.
