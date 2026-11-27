# Exceptionless .NET Client Configuration And Runtime Settings


## Configuration Order

Configure before the first event or queue operation. Current source locks important values after initialization/queue use:

- `ApiKey`
- `ServerUrl`
- `ConfigServerUrl`
- `HeartbeatServerUrl`

Startup/configuration paths:

- Root `Startup()` calls `ReadAllConfig()`, `UseTraceLogEntriesPlugin()`, unhandled exception handlers, process-exit flushing, and task scheduler handlers.
- ASP.NET Core/Hosting `AddExceptionless(...)` reads `IConfiguration` and applies your lambda.
- Legacy .NET Framework reads app.config/web.config, app settings, attributes, environment variables, and saved server settings.
- Server settings can update the local `Settings` dictionary after startup.

## Deployment Environment

`Event.Environment` (root `environment` field) drives server filtering, per-environment notifications and the project environment allow-list. It is separate from machine diagnostics in `data.@environment`; do not put environments in `DefaultTags`/`DefaultData`.

The first match in this order wins:

1. **Per event:** `builder.SetEnvironment("staging")`.
2. **Code:** `config.SetEnvironment("production")` / `config.Environment`.
3. **Configuration** (last source read wins):
   - `Exceptionless:Environment` (IConfiguration or app setting)
   - `Exceptionless__Environment` env var
   - `[assembly: Exceptionless(..., Environment = "...")]`
   - `<exceptionless environment="..." />`
4. **Host:** `IHostEnvironment.EnvironmentName`. Applied by the hosting, ASP.NET Core and lifetime-service integrations.
5. **Process:** the first of `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT` or `AZURE_FUNCTIONS_ENVIRONMENT` that is set.

Validation:

- Names are trimmed and must match `^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$`. Casing is kept; the server matches case-insensitively.
- A configured invalid or empty value leaves events unspecified instead of falling back.

## Code-First Configuration

```csharp
using System;
using Exceptionless;
using Exceptionless.Logging;

var client = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.ServerUrl = "https://collector.exceptionless.io";
    c.SetVersion("2.4.1");
    c.SetEnvironment("production");
    c.DefaultData["Service"] = "checkout";
    c.Settings["FeatureXYZEnabled"] = "false";
    c.IncludePrivateInformation = false;
    c.UseFolderStorage("exceptionless-queue");
    c.UseFileLogger("exceptionless-client.log", LogLevel.Info);
});

client.Startup();
client.SubmitFeatureUsage("Checkout.Started");
await client.ProcessQueueAsync();
```

## appsettings.json

Current `ReadFromConfiguration` supports these keys: `Enabled`, `ApiKey`, `Environment`, `ServerUrl`, `QueueMaxAge`, `QueueMaxAttempts`, `StoragePath`, `StorageSerializer`, `EnableLogging`, `LogPath`, `IncludePrivateInformation`, `ProcessQueueOnCompletedRequest`, `DefaultTags`, `DefaultData`, and `Settings`.

```json
{
  "Exceptionless": {
    "ApiKey": "VALID_API_KEY_12345",
    "ServerUrl": "https://collector.exceptionless.io",
    "Environment": "production",
    "Enabled": true,
    "StoragePath": "exceptionless-queue",
    "EnableLogging": true,
    "LogPath": "exceptionless-client.log",
    "IncludePrivateInformation": false,
    "ProcessQueueOnCompletedRequest": true,
    "DefaultTags": [ "api" ],
    "DefaultData": {
      "Service": "checkout",
      "Region": "us-central"
    },
    "Settings": {
      "FeatureXYZEnabled": false,
      "@@log:*": "Warn"
    }
  }
}
```

## Environment Variables

Both colon and double-underscore names are supported for these current keys:

```text
Exceptionless:ApiKey=VALID_API_KEY_12345
Exceptionless__ApiKey=VALID_API_KEY_12345
Exceptionless:Environment=production
Exceptionless__Environment=production
Exceptionless:Enabled=true
Exceptionless__Enabled=true
Exceptionless:ServerUrl=https://collector.exceptionless.io
Exceptionless__ServerUrl=https://collector.exceptionless.io
Exceptionless:ProcessQueueOnCompletedRequest=true
Exceptionless__ProcessQueueOnCompletedRequest=true
```

## app.config And web.config

Legacy .NET Framework config section:

```xml
<configuration>
  <configSections>
    <section name="exceptionless" type="Exceptionless.ExceptionlessSection, Exceptionless" />
  </configSections>
  <exceptionless
    apiKey="VALID_API_KEY_12345"
    serverUrl="https://collector.exceptionless.io"
    environment="production"
    enabled="true"
    includePrivateInformation="false"
    tags="api"
    storagePath="C:\Exceptionless\Queue"
    enableLogging="true"
    logPath="C:\Exceptionless\exceptionless.log">
    <data>
      <add name="Service" value="checkout" />
      <add name="Region" value="us-central" />
    </data>
    <settings>
      <add name="FeatureXYZEnabled" value="false" />
      <add name="@@log:*" value="Warn" />
    </settings>
  </exceptionless>
</configuration>
```

Assembly attributes:

```text
[assembly: Exceptionless.Configuration.Exceptionless("VALID_API_KEY_12345", ServerUrl = "https://collector.exceptionless.io", Environment = "production")]
[assembly: Exceptionless.Configuration.ExceptionlessSetting("FeatureXYZEnabled", "false")]
```

## Self-Hosted Exceptionless

Set `ServerUrl` before startup. Current source also assigns `ConfigServerUrl` and `HeartbeatServerUrl` when `ServerUrl` is set.

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.ServerUrl = "https://exceptionless.example.com";
ExceptionlessClient.Default.Startup();
ExceptionlessClient.Default.SubmitFeatureUsage("SelfHosted.Startup");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

## Queue Storage And Batching

Defaults: queue max age 7 days, max attempts 3, submission batch size 50.

```csharp
using System;
using Exceptionless;

var durableClient = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.QueueMaxAge = TimeSpan.FromDays(3);
    c.QueueMaxAttempts = 5;
    c.SubmissionBatchSize = 25;
    c.UseFolderStorage("exceptionless-queue");
});

durableClient.SubmitLog("Durable queue configured.");
await durableClient.ProcessQueueAsync();
```

Use folder storage for durability. Use in-memory storage for high-throughput or logging-only paths when losing unsent events is acceptable.

## Shutdown And Flushing

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

`ShutdownAsync()` unregisters handlers, processes the queue, sends a session-end heartbeat when sessions are enabled, and flushes the client log.

## Sessions And User Identity

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.UseSessions(sendHeartbeats: true);
ExceptionlessClient.Default.Configuration.SetUserIdentity("user-123", "Ada Lovelace");
ExceptionlessClient.Default.Startup();

ExceptionlessClient.Default.SubmitSessionStart();
await ExceptionlessClient.Default.SubmitSessionEndAsync();
```

Anonymous session events are cancelled by the default `CancelSessionsWithNoUserPlugin`.

## Real-Time Server Settings

The client periodically retrieves project settings and applies them to `ExceptionlessConfiguration.Settings`.

Important behavior:

- Settings may update when submission responses report a newer settings version.
- With idle polling enabled and no events, the initial settings check starts after 5 seconds.
- Positive `UpdateSettingsWhenIdleInterval` values below 2 minutes are clamped to 2 minutes.
- Zero or negative disables idle polling.
- Call `SettingsManager.UpdateSettingsAsync(config)` to force an update.

```csharp
using System;
using Exceptionless;
using Exceptionless.Configuration;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.UpdateSettingsWhenIdleInterval = TimeSpan.FromMinutes(2);

await SettingsManager.UpdateSettingsAsync(ExceptionlessClient.Default.Configuration);

ExceptionlessClient.Default.Configuration.UpdateSettingsWhenIdleInterval = TimeSpan.Zero;
```

## Certificate Options

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.ServerUrl = "https://exceptionless.internal";
ExceptionlessClient.Default.Configuration.TrustCertificateThumbprint("86481791CDAF6D7A02BEE9A649EA9F84DE84D22C");
ExceptionlessClient.Default.Startup();
ExceptionlessClient.Default.SubmitLog("Certificate pin configured.");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

Do not use `SkipCertificateValidation()` in production.
