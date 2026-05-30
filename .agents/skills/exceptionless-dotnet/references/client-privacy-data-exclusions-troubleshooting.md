# Exceptionless .NET Privacy Data Exclusions Troubleshooting And Upgrades


## Privacy Baseline

By default, the .NET client can collect metadata that may include private information. Production integrations should explicitly choose:

- `IncludePrivateInformation`
- data exclusion patterns
- whether to include user name, machine name, IP address, headers, cookies, POST data, and query strings
- what custom objects are attached with `AddObject` or `SetProperty`
- which plugins redact domain-specific sensitive data

## Data Exclusions

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.AddDataExclusions(
    "Password",
    "Password*",
    "*Token*",
    "*CreditCard*",
    "Authorization");

ExceptionlessClient.Default.SubmitLog("Data exclusions configured.");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

Remote/server-side exclusions arrive through `@@DataExclusions` and are unioned with local exclusions.

## Fine-Grained Metadata Controls

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.IncludePrivateInformation = false;

ExceptionlessClient.Default.Configuration.IncludeUserName = false;
ExceptionlessClient.Default.Configuration.IncludeMachineName = false;
ExceptionlessClient.Default.Configuration.IncludeIpAddress = false;
ExceptionlessClient.Default.Configuration.IncludeHeaders = false;
ExceptionlessClient.Default.Configuration.IncludeCookies = false;
ExceptionlessClient.Default.Configuration.IncludePostData = false;
ExceptionlessClient.Default.Configuration.IncludeQueryString = false;

ExceptionlessClient.Default.SubmitLog("Privacy controls configured.");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

`IncludePrivateInformation = false` sets the individual flags false in current source.

## Plugin Redaction

```csharp
using System.Linq;
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.AddPlugin("remove-sensitive-extended-data", 5, context => {
    foreach (string key in context.Event.Data.Keys.ToList()) {
        if (key.Contains("Secret") || key.Contains("Token"))
            context.Event.Data.Remove(key);
    }
});

ExceptionlessClient.Default.CreateLog("Privacy", "Redaction plugin active.")
    .SetProperty("SecretValue", "removed before submit")
    .Submit();

await ExceptionlessClient.Default.ProcessQueueAsync();
```

Use early plugin priority for redaction.

## Diagnostic Logging

```csharp
using Exceptionless;
using Exceptionless.Logging;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.UseFileLogger("exceptionless-client.log", LogLevel.Trace);
ExceptionlessClient.Default.Startup();

ExceptionlessClient.Default.SubmitLog("Diagnostics enabled.");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

Legacy config equivalent:

```xml
<exceptionless apiKey="VALID_API_KEY_12345" enableLogging="true" logPath="C:\exceptionless.log" />
```

## Missing Event Troubleshooting

Check in order:

1. API key is set, is not `API_KEY_HERE`, is at least 10 characters, and has no spaces.
2. `Enabled` is true.
3. `ServerUrl`, `ConfigServerUrl`, and `HeartbeatServerUrl` are non-empty and reachable.
4. The platform startup/registration method ran before submission.
5. The process had time to process the queue, or `ProcessQueueAsync()` ran before exit.
6. Storage path exists and the process identity can read/write it.
7. Remote settings are not filtering by log level, event type/source, exception type, or custom settings.
8. Plugins, event exclusions, or `SubmittingEvent` handlers are not cancelling.
9. Proxy/firewall rules allow collector/config/heartbeat endpoints.
10. Client diagnostic logs do not show serialization, storage, certificate, or submission errors.

## Proxy And Firewall Checks

Allow SaaS endpoints:

```text
https://collector.exceptionless.io
https://config.exceptionless.io
https://heartbeat.exceptionless.io
```

.NET proxy config example:

```xml
<system.net>
  <defaultProxy useDefaultCredentials="true">
    <proxy proxyaddress="http://proxy.example.com:8080" usesystemdefault="true" />
  </defaultProxy>
  <bypasslist>
    <add address="[a-z]+\.exceptionless\.io$" />
  </bypasslist>
</system.net>
```

Or set `ExceptionlessClient.Default.Configuration.Proxy` before startup.

## De-Duplication

The default `DuplicateCheckerPlugin` runs late in the local pipeline. Exceptionless also groups similar events server-side. Prefer log-level settings, type/source settings, `AddEventExclusion`, or domain-aware plugins instead of custom de-duplication unless required.

## Upgrade Notes

- From 5.x: known Exceptionless models serialize with snake case while custom data preserves user casing; empty collections/dictionaries are preserved.
- From 4.x: synchronous queue/submission APIs moved to async names such as `ProcessQueueAsync`, `ShutdownAsync`, `SubmitSessionEndAsync`, and `SettingsManager.UpdateSettingsAsync`.
- Replace old deferred queue processing helpers with an async-disposable scope calling `ProcessQueueAsync`.
- From 3.x: `Exceptionless.Portable` and `Exceptionless.Extras` merged into `Exceptionless`.
- From 2.x: enrichments became plugins (`IEventEnrichment` to `IEventPlugin`, `Enrich` to `Run`, `AddEnrichment` to `AddPlugin`, `Data` to `ContextData`).

## Production Checklist

- Correct package selected.
- API key comes from secret/config management.
- Self-hosted URLs set before startup if applicable.
- Privacy and data exclusions are explicit.
- Startup method is called once and before event submission.
- Queue storage selected for durability versus throughput.
- Queue flush path exists for shutdown, CLI exit, and serverless return.
- Runtime settings and log-level strategy are documented.
- Client diagnostics can be enabled without code changes.
- Plugins are keyed, prioritized, tested, and removable.
