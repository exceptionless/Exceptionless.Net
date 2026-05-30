# Exceptionless .NET Plugins And Event Pipeline


## Event Pipeline Model

Every submitted event runs through registered plugins before queueing. Plugins receive `EventPluginContext`, which exposes `Client`, `Event`, `ContextData`, `Resolver`, `Log`, and `Cancel`. Set `context.Cancel = true` to stop submission. Plugins run by ascending priority; cancellation stops the remaining pipeline.

## Default Plugins

| Plugin | Priority | Purpose |
| --- | ---: | --- |
| `HandleAggregateExceptionsPlugin` | 5 | Handles aggregate exceptions |
| `EventExclusionPlugin` | 10 | Applies callbacks, log levels, type/source filters, exception filters |
| `ConfigurationDefaultsPlugin` | 15 | Adds default tags/data |
| `ErrorPlugin` | 20 | Builds error data from exception context |
| `EnvironmentInfoPlugin` | 50 | Adds environment information |
| `VersionPlugin` | 80 | Adds version information |
| `SubmissionMethodPlugin` | 90 | Records submission method |
| `CancelSessionsWithNoUserPlugin` | 900 | Cancels anonymous session events |
| `DuplicateCheckerPlugin` | 910 | Locally de-duplicates identical events |

Platform packages add more plugins such as ASP.NET Core/web request context, user-agent bot filtering, reference IDs, sessions, and trace log entries.

## Plugin Priorities

Use priorities intentionally:

- `0-9`: very early cancellation or redaction before built-in exclusion/error handling.
- `10-90`: enrichment near built-in processing.
- `100+`: post-enrichment logic.
- `900+`: late cancellation/de-duplication style behavior.

If no priority is supplied, source currently defaults to `0`.

## Action Plugins

```csharp
using Exceptionless;
using Exceptionless.Models;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";

ExceptionlessClient.Default.Configuration.AddPlugin("conditionally-cancel-logs", 100, context => {
    bool enableLogSubmission = context.Client.Configuration.Settings.GetBoolean("enableLogSubmission", true);
    if (context.Event.Type == Event.KnownTypes.Log && !enableLogSubmission)
        context.Cancel = true;
});

ExceptionlessClient.Default.Configuration.AddPlugin("tenant-enrichment", 100, context => {
    if (context.Client.Configuration.Settings.GetBoolean("includeTenantData", true))
        context.Event.SetProperty("TenantId", "tenant-a");
});

ExceptionlessClient.Default.SubmitLog("Plugin pipeline configured.");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

Remove by key:

```text
ExceptionlessClient.Default.Configuration.RemovePlugin("tenant-enrichment")
```

## Class Plugins

```csharp
using System;
using Exceptionless;
using Exceptionless.Models;
using Exceptionless.Plugins;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.AddPlugin<FeatureUptimePlugin>();

ExceptionlessClient.Default.SubmitFeatureUsage("Search");
await ExceptionlessClient.Default.ProcessQueueAsync();

[Priority(100)]
public sealed class FeatureUptimePlugin : IEventPlugin {
    public void Run(EventPluginContext context) {
        if (context.Event.Type != Event.KnownTypes.FeatureUsage)
            return;

        context.Event.SetProperty("ProcessUptime", TimeSpan.FromMilliseconds(Environment.TickCount64).ToString());
    }
}
```

Use class plugins for reusable behavior, tests, dependency resolver usage, or complex redaction/enrichment.

## Runtime Setting-Driven Plugins

Use server-synced settings inside plugins when operations needs remote switches without redeploying.

```csharp
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.Settings["IncludeConditionalData"] = "true";

ExceptionlessClient.Default.Configuration.AddPlugin("conditional-order-data", 100, context => {
    if (context.Client.Configuration.Settings.GetBoolean("IncludeConditionalData", true))
        context.Event.AddObject(new { Total = 32.34m, ItemCount = 2 }, "ConditionalData");
});

ExceptionlessClient.Default.SubmitFeatureUsage("PluginRuntimeSettings");
await ExceptionlessClient.Default.ProcessQueueAsync();
```

## Event Exclusions

`AddEventExclusion` callbacks return `true` to keep the event and `false` to cancel it.

```csharp
using Exceptionless;

var client = new ExceptionlessClient("VALID_API_KEY_12345");

client.Configuration.AddEventExclusion(ev => {
    if (ev.Value == 0)
        return false;

    return true;
});

client.CreateFeatureUsage("ExpensiveFeature").SetValue(0).Submit();
await client.ProcessQueueAsync();
```

Use `AddEventExclusion` for simple allow/deny predicates. Use plugins for redaction, enrichment, or behavior that needs `ContextData`.

## Settings Change Notifications

```csharp
using System.Diagnostics;
using Exceptionless;

ExceptionlessClient.Default.Configuration.ApiKey = "VALID_API_KEY_12345";
ExceptionlessClient.Default.Configuration.Settings.Changed += (_, args) => {
    Trace.WriteLine($"Setting {args.Item.Key} changed via {args.Action}.");
};

ExceptionlessClient.Default.Configuration.Settings["IncludeOrderData"] = "true";
```
