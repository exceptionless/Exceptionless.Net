# Exceptionless .NET Package Selection


## Choose The Package

| App or use case | Package | Startup surface |
| --- | --- | --- |
| Console, CLI, simple non-visual service | `Exceptionless` | `ExceptionlessClient.Default.Startup(...)` or `new ExceptionlessClient(...)` |
| ASP.NET Core web app/API (including minimal APIs) | `Exceptionless.AspNetCore` | `builder.AddExceptionless(...)`, `app.UseExceptionHandler()`, `app.UseExceptionless()` |
| Generic Host worker/service | `Exceptionless.Extensions.Hosting` | `builder.AddExceptionless(...)`, `builder.UseExceptionless()` |
| Microsoft `ILogger` provider | `Exceptionless.Extensions.Logging` | `builder.Logging.AddExceptionless(...)` |
| ASP.NET MVC 3+ | `Exceptionless.Mvc` | web.config module from package |
| ASP.NET Web API | `Exceptionless.WebApi` | `ExceptionlessClient.Default.RegisterWebApi(config)` |
| ASP.NET WebForms/WCF | `Exceptionless.Web` | web.config module; `[ExceptionlessWcfHandleError]` for WCF |
| WPF | `Exceptionless.Wpf` | `ExceptionlessClient.Default.Register()` |
| Windows Forms | `Exceptionless.Windows` | `ExceptionlessClient.Default.Register()` |
| NLog | `Exceptionless.NLog` | `ExceptionlessTarget` |
| log4net | `Exceptionless.Log4net` | `ExceptionlessAppender` |
| Durable/faster queue serialization | `Exceptionless.MessagePack` | `c.UseMessagePackSerializer()` |
| Serilog | `Serilog.Sinks.ExceptionLess` | External community package, not in this repository |

Prefer platform packages over the root package when they exist. They add context collectors, middleware/modules, exception handlers, and lifecycle behavior the root package does not know about.

## Current Source Files

- Root client: `src/Exceptionless/ExceptionlessClient.cs`, `src/Exceptionless/Extensions/ExceptionlessClientExtensions.cs`, `src/Exceptionless/Configuration/ExceptionlessConfiguration.cs`.
- Configuration helpers: `src/Exceptionless/Extensions/ExceptionlessConfigurationExtensions.cs`, `src/Exceptionless/Configuration/SettingsManager.cs`, `src/Exceptionless/Models/Collections/SettingsDictionary.cs`.
- Fluent events: `src/Exceptionless/EventBuilder.cs`, `src/Exceptionless/Extensions/EventBuilderExtensions.cs`, `src/Exceptionless/Extensions/ExceptionExtensions.cs`.
- Plugins: `src/Exceptionless/Plugins/EventPluginManager.cs`, `src/Exceptionless/Plugins/Default/*`.
- ASP.NET Core: `src/Platforms/Exceptionless.AspNetCore/ExceptionlessExtensions.cs`, `ExceptionlessMiddleware.cs`, `ExceptionlessExceptionHandler.cs`.
- Hosting: `src/Platforms/Exceptionless.Extensions.Hosting/ExceptionlessExtensions.cs`, `ExceptionlessLifetimeService.cs`.
- Logging: `src/Platforms/Exceptionless.Extensions.Logging/ExceptionlessLoggerExtensions.cs`, `ExceptionlessLoggerProvider.cs`, `ExceptionlessLogger.cs`.
- NLog/log4net: `src/Platforms/Exceptionless.NLog/*`, `src/Platforms/Exceptionless.Log4net/*`.
- Legacy ASP.NET: `src/Platforms/Exceptionless.Mvc/*`, `Exceptionless.WebApi/*`, `Exceptionless.Web/*`.
- Desktop: `src/Platforms/Exceptionless.Wpf/*`, `src/Platforms/Exceptionless.Windows/*`.
- MessagePack: `src/Platforms/Exceptionless.MessagePack/ExceptionlessConfigurationExtensions.cs`.

## Public Documentation Sources

- .NET client landing page: https://exceptionless.com/docs/clients/dotnet/
- Configuration: https://exceptionless.com/docs/clients/dotnet/configuration/
- Client configuration values: https://exceptionless.com/docs/clients/dotnet/client-configuration-values/
- Platform guides: https://exceptionless.com/docs/clients/dotnet/guides/
- Console guide: https://exceptionless.com/docs/clients/dotnet/guides/console-apps-example/
- Web server guide: https://exceptionless.com/docs/clients/dotnet/guides/web-server-example/
- Generic Host logging guide: https://exceptionless.com/docs/clients/dotnet/guides/logging-with-generic-host/
- Sending events: https://exceptionless.com/docs/clients/dotnet/sending-events/
- Supported platforms: https://exceptionless.com/docs/clients/dotnet/supported-platforms/
- Settings: https://exceptionless.com/docs/clients/dotnet/settings/
- Plugins: https://exceptionless.com/docs/clients/dotnet/plugins/
- Private information: https://exceptionless.com/docs/clients/dotnet/private-information/
- Troubleshooting: https://exceptionless.com/docs/clients/dotnet/troubleshooting/
- Upgrading: https://exceptionless.com/docs/clients/dotnet/upgrading/
- Log levels: https://exceptionless.com/docs/setting-log-levels/
- Security/data exclusions: https://exceptionless.com/docs/security/
- Event de-duplication: https://exceptionless.com/docs/deduplication/
- Agent Skills specification: https://agentskills.io/specification
- Agent Skills description guidance: https://agentskills.io/skill-creation/optimizing-descriptions

## Current-Source Notes That Override Older Docs

- Current ASP.NET Core setup uses `builder.AddExceptionless(...)` and the built-in `IExceptionHandler`; older docs show `app.UseExceptionless(Configuration)`, which is stale for current source.
- ASP.NET Core still needs `builder.Services.AddProblemDetails()` or an equivalent response handler plus `app.UseExceptionHandler()` to activate the exception-handler pipeline.
- `app.UseExceptionless()` starts the client, adds request/diagnostic plugins, tracks 404s, and can flush the queue on request completion.
- `Exceptionless.Extensions.Hosting` has .NET 8+ `IHostApplicationBuilder` overloads and registers a lifetime service to flush on host shutdown.
- `Exceptionless.Extensions.Logging` sets a default Exceptionless minimum log level of `Trace` so Microsoft logging rules can decide what reaches the provider before remote settings arrive.
- `UpdateSettingsWhenIdleInterval` is clamped to at least 2 minutes when positive. Zero or negative disables idle polling.
- `ApiKey`, `ServerUrl`, `ConfigServerUrl`, and `HeartbeatServerUrl` cannot be changed after the configuration is locked by queue/submission startup.
