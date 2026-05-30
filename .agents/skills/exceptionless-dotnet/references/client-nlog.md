# Exceptionless.NLog Target


## When To Use

Use `Exceptionless.NLog` when an application already uses NLog and should send NLog events to Exceptionless. It can read the root Exceptionless configuration or accept an API key on the target.

## Install

```bash
dotnet add package Exceptionless.NLog
```

## XML Configuration

Set NLog `minlevel` to `Trace` if Exceptionless server-side project settings should decide final filtering. Set it higher if NLog should discard lower-level logs before Exceptionless sees them.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<nlog xmlns="http://www.nlog-project.org/schemas/NLog.xsd"
      xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <targets async="true">
    <target type="Exceptionless, Exceptionless.NLog" name="exceptionless" apiKey="VALID_API_KEY_12345">
      <field name="host" layout="${machinename}" />
      <field name="process" layout="${processname}" />
      <field name="user" layout="${environment-user}" />
    </target>
  </targets>
  <rules>
    <logger name="*" minlevel="Trace" writeTo="exceptionless" />
  </rules>
</nlog>
```

## Code Configuration

Current sample configures `ExceptionlessTarget` in code:

```text
var config = new LoggingConfiguration();
var exceptionlessTarget = new ExceptionlessTarget();
config.AddTarget("exceptionless", exceptionlessTarget);
config.LoggingRules.Add(new LoggingRule("*", NLog.LogLevel.Debug, exceptionlessTarget));
LogManager.Configuration = config;
```

## Fields And Structured Data

The target supports `ExceptionlessField` entries. Use fields for low-cardinality operational data such as host, process, environment, region, and service. Avoid credentials, raw request payloads, and high-cardinality IDs unless they are needed for investigation.

The package also exposes fluent helpers in `Exceptionless.NLog`:

```text
logger.ForWarnEvent()
  .Message("App starting")
  .Tag("startup")
  .Property("LocalProp", "LocalValue")
  .Property("Order", new { Total = 15 })
  .Log()
```

## Log Level Strategy

There are two filtering layers:

- NLog rules decide which log events reach the Exceptionless target.
- Exceptionless settings such as `@@log:*` and `@@log:Namespace.Type` decide which reached events are queued/submitted.

If support needs near-real-time remote log-level changes, keep NLog permissive enough, usually `Trace`, and control effective levels through Exceptionless settings.

## Best Practices

- Use async NLog targets for production.
- Prefer remote log-level settings for temporary debugging.
- Add fields for environment and service ownership.
- Keep target-level API key configuration out of source-controlled XML when possible; use transforms, environment-specific config, or root Exceptionless configuration.
