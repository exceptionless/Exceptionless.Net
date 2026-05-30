# Exceptionless.AspNetCore Client


## When To Use

Use `Exceptionless.AspNetCore` for ASP.NET Core web apps and APIs. It integrates with DI, ASP.NET Core exception handling, request diagnostics, 404 tracking, HTTP context collection, and host shutdown flushing. Add `Exceptionless.Extensions.Logging` when `ILogger` events should be sent too.

## Install

```bash
dotnet add package Exceptionless.AspNetCore
dotnet add package Exceptionless.Extensions.Logging
```

## Current Minimal Setup

Current source setup is newer than the public web-server example. Use `WebApplicationBuilder.AddExceptionless`, `AddProblemDetails`, `UseExceptionHandler`, and `UseExceptionless`.

```csharp
using Exceptionless;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddExceptionless();
builder.AddExceptionless(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.DefaultData["Service"] = "payments-api";
    c.DefaultTags.Add("aspnetcore");
});

builder.Services.AddProblemDetails();
builder.Services.AddControllers();

var app = builder.Build();

app.UseExceptionHandler();
app.UseExceptionless();

app.MapControllers();
app.Run();
```

## Minimal APIs

Minimal APIs are supported using the same middleware stack as controllers; register `ProblemDetails`, `UseExceptionHandler`, and `UseExceptionless` before endpoint mapping, and inject `ExceptionlessClient` into handlers as needed.

```csharp
using Exceptionless;
using Exceptionless.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddExceptionless();
builder.AddExceptionless(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.DefaultData["Service"] = "minimal-api";
    c.DefaultTags.Add("minimal-api");
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseExceptionless();

app.MapGet("/ping", (ExceptionlessClient exceptionless) => {
    exceptionless.SubmitFeatureUsage("PingEndpoint");
    return Results.Ok(new { ok = true, service = "minimal-api" });
});

app.MapGet("/fail", (HttpContext context) => throw new InvalidOperationException("Handled by middleware"));

app.Run();
```

## Controller And Service Usage

Inject `ExceptionlessClient`. The static default works, but DI is better for tests and code clarity.

```csharp
using System;
using Exceptionless;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

[ApiController]
[Route("api/[controller]")]
public sealed class ValuesController : ControllerBase {
    private readonly ExceptionlessClient _exceptionless;
    private readonly ILogger<ValuesController> _logger;

    public ValuesController(ExceptionlessClient exceptionless, ILogger<ValuesController> logger) {
        _exceptionless = exceptionless;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id) {
        _logger.LogWarning("Loading value {ValueId}", id);
        _exceptionless.SubmitFeatureUsage("ValuesController.Get");

        try {
            throw new InvalidOperationException("Example handled failure.");
        } catch (Exception ex) {
            ex.ToExceptionless(_exceptionless)
                .SetProperty("ValueId", id)
                .AddTags("handled")
                .Submit();
        }

        return Ok(new { id });
    }
}
```

## Middleware Behavior

`app.UseExceptionless()`:

- Resolves the DI client or falls back to `ExceptionlessClient.Default`.
- Calls `client.Startup()`.
- Adds `ExceptionlessAspNetCorePlugin` and `IgnoreUserAgentPlugin`.
- Subscribes to relevant diagnostic listener events.
- Registers shutdown queue flushing if the hosting lifetime service is not already registered.
- Adds middleware that records 404 responses.
- Flushes the queue on response completion when `ProcessQueueOnCompletedRequest` is true.

## ASP.NET Core Configuration

```json
{
  "Exceptionless": {
    "ApiKey": "VALID_API_KEY_12345",
    "ServerUrl": "https://collector.exceptionless.io",
    "IncludePrivateInformation": false,
    "ProcessQueueOnCompletedRequest": false,
    "DefaultData": {
      "Service": "payments-api"
    },
    "DefaultTags": [ "aspnetcore", "production" ],
    "Settings": {
      "@@log:*": "Warn",
      "enableLogSubmission": true
    }
  }
}
```

## Serverless ASP.NET Core

For AWS Lambda/Azure Functions style ASP.NET Core hosting, set `ProcessQueueOnCompletedRequest` when each request must flush before the runtime freezes. This is safer but more expensive per request.

## Best Practices

- Keep `UseExceptionHandler()` before endpoints; call `UseExceptionless()` before endpoint mapping.
- Add `AddProblemDetails()` or a custom exception response handler so HTTP errors get responses.
- Prefer `ILogger` for normal application logs and `ExceptionlessClient` for feature usage, not-found, custom events, and handled exceptions with rich context.
- Include privacy configuration in `appsettings.json`, not only in code.
- Use `SetHttpContext` for manual events outside the normal request pipeline when request info matters.
