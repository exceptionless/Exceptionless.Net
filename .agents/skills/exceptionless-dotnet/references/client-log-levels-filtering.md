# Exceptionless .NET Log Levels And Event Filtering


## Log Level Model

Exceptionless log levels are ordered:

```text
Trace < Debug < Info < Warn < Error < Fatal < Off
```

`Other` is used for unrecognized values and is not filtered as a normal level. Boolean-like values map too:

- `true`, `yes`, `1` => `Trace`
- `false`, `no`, `0` => `Off`

## Microsoft Logging Mapping

| Microsoft | Exceptionless |
| --- | --- |
| `Trace` | `Trace` |
| `Debug` | `Debug` |
| `Information` | `Info` |
| `Warning` | `Warn` |
| `Error` | `Error` |
| `Critical` | `Fatal` |
| `None` | `Off` |

## Remote Log-Level Settings

Use `@@log:` setting keys. Source matches the log source/category.

```csharp
using Exceptionless;
using Exceptionless.Logging;
using Exceptionless.Models;

var client = new ExceptionlessClient("VALID_API_KEY_12345");

client.Configuration.Settings[SettingsDictionary.KnownKeys.LogLevelPrefix + "*"] = "Warn";
client.Configuration.Settings[SettingsDictionary.KnownKeys.LogLevelPrefix + "Checkout"] = "Debug";

client.SubmitLog("Checkout", "Debug message sent for Checkout only.", LogLevel.Debug);
client.SubmitLog("Inventory", "Debug message filtered by global Warn.", LogLevel.Debug);

await client.ProcessQueueAsync();
```

Use the Exceptionless app/server project settings UI for near-real-time changes. Use local defaults for bootstrapping before server settings arrive.

## Type/Source Event Filters

`EventExclusionPlugin` checks `@@{type}:{sourcePattern}` for non-log events.

```text
@@usage:* = false
@@usage:ExperimentalFeature = false
@@404:* = false
@@404:*.php = false
@@404:/old-url = false
```

Source patterns support wildcards.

## Exception Type Filters

Exception filtering also uses type/source settings with the error event type and exception full name.

```text
@@error:System.InvalidOperationException = false
@@error:*TimeoutException = false
@@error:* = false
```

Use sparingly; filtering all errors can hide outages.

## User Agent Bot Patterns

`IgnoreUserAgentPlugin` cancels events whose request user agent matches configured bot patterns.

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.AddUserAgentBotPatterns("*Bot*", "*Crawler*", "HealthCheck/*");
ExceptionlessClient.Default.SubmitLog("Bot patterns configured.");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

Server settings can also provide `@@UserAgentBotPatterns`.

## High-Throughput Logging

For very noisy logs:

- Keep Microsoft/NLog/log4net source filtering tight.
- Use Exceptionless remote settings for temporary diagnostics.
- Use `UseInMemoryStorage()` for log-only clients when durability is less important than throughput.
- Use `SubmissionBatchSize` deliberately.
- Prefer structured fields with bounded cardinality.

```csharp
using Exceptionless;

var logClient = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.UseInMemoryStorage();
    c.SubmissionBatchSize = 100;
});

logClient.SubmitLog("Fast log path.");
await logClient.ProcessQueueAsync();
```
