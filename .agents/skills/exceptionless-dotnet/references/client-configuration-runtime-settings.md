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

## Code-First Configuration

```csharp
using System;
using Exceptionless;
using Exceptionless.Logging;

var client = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.ServerUrl = "https://collector.exceptionless.io";
    c.SetVersion("2.4.1");
    c.DefaultTags.Add("production");
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

Current `ReadFromConfiguration` supports these keys: `Enabled`, `ApiKey`, `ServerUrl`, `QueueMaxAge`, `QueueMaxAttempts`, `StoragePath`, `StorageSerializer`, `EnableLogging`, `LogPath`, `IncludePrivateInformation`, `ProcessQueueOnCompletedRequest`, `DefaultTags`, `DefaultData`, and `Settings`.

```json
{
  "Exceptionless": {
    "ApiKey": "VALID_API_KEY_12345",
    "ServerUrl": "https://collector.exceptionless.io",
    "Enabled": true,
    "StoragePath": "exceptionless-queue",
    "EnableLogging": true,
    "LogPath": "exceptionless-client.log",
    "IncludePrivateInformation": false,
    "ProcessQueueOnCompletedRequest": true,
    "DefaultTags": [ "api", "production" ],
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
    enabled="true"
    includePrivateInformation="false"
    tags="api,production"
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
[assembly: Exceptionless.Configuration.Exceptionless("VALID_API_KEY_12345", ServerUrl = "https://collector.exceptionless.io")]
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
