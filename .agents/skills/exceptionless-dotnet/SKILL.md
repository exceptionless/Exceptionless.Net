---
name: exceptionless-dotnet
description: Use this skill when configuring, integrating, documenting, or reviewing Exceptionless .NET clients. Covers package selection and setup for Exceptionless, Exceptionless.AspNetCore, Extensions.Hosting, Extensions.Logging, MVC, Web API, WebForms, WCF, WPF, Windows Forms, NLog, log4net, Blazor, and serverless; client configuration, appsettings/app.config/environment variables, self-hosted URLs, storage, queue flushing, events, log levels, server-synced settings, plugins, privacy/data exclusions, troubleshooting, and upgrades.
---

# Exceptionless .NET

## Core Workflow

Use this skill to produce implementation-ready Exceptionless .NET guidance or patches. Prefer current repository source and package readmes over older public docs when they differ.

1. Identify the app shape first: ASP.NET Core, Generic Host worker/service, console, legacy ASP.NET MVC/Web API/WebForms/WCF, WPF, Windows Forms, logging-only integration, Blazor WebAssembly, or serverless.
2. Read only the reference file that matches the requested package or advanced topic:
   - `references/client-package-selection.md` for package choice, authoritative source files, and public docs.
   - `references/client-console-services.md` for the root `Exceptionless` package in console apps, services, Blazor WebAssembly, and serverless functions.
   - `references/client-aspnetcore.md` for `Exceptionless.AspNetCore`.
   - `references/client-extensions-hosting.md` for `Exceptionless.Extensions.Hosting`.
   - `references/client-extensions-logging.md` for `Exceptionless.Extensions.Logging`.
   - `references/client-nlog.md` for `Exceptionless.NLog`.
   - `references/client-log4net.md` for `Exceptionless.Log4net`.
   - `references/client-mvc.md`, `references/client-webapi.md`, and `references/client-web-webforms-wcf.md` for legacy ASP.NET packages.
   - `references/client-wpf.md` and `references/client-windows-forms.md` for desktop packages.
   - `references/client-messagepack.md` for `Exceptionless.MessagePack`.
   - `references/client-configuration-runtime-settings.md` for API keys, config sources, self-hosted URLs, storage, real-time settings, sessions, and serverless queue flushing.
   - `references/client-plugins-event-pipeline.md` for plugins, priorities, cancellation, enrichment, and event exclusions.
   - `references/client-log-levels-filtering.md` for log levels, remote log filters, type/source filters, and high-throughput logging.
   - `references/client-privacy-data-exclusions-troubleshooting.md` for privacy, diagnostics, troubleshooting, proxies, de-duplication, and upgrades.
3. Choose the platform package before writing code. Do not use the root `Exceptionless` package alone when a platform package will capture richer context automatically.
4. Configure before the first event is submitted. Some settings are locked after queue/submission startup, including API key and server URLs.
5. Prefer dependency injection for ASP.NET Core and hosted services. Use `ExceptionlessClient.Default` for simple console apps, legacy integrations, and examples only when DI is not already available.
6. Treat privacy as a first-class requirement. Default collection includes potentially private metadata; explicitly decide on `IncludePrivateInformation`, data exclusions, and custom plugin redaction for production examples.
7. When changing code examples, update or add repository tests that exercise the documented API surface. Do not add executable scripts, generated validators, or other runnable code inside this skill.

## Quality Bar

- Include the package name, startup call, configuration source, queue-flush behavior, and privacy stance for every setup recommendation.
- For ASP.NET Core on current source, include `builder.AddExceptionless(...)`, `builder.Services.AddProblemDetails()`, `app.UseExceptionHandler()`, and `app.UseExceptionless()` when capturing unhandled HTTP exceptions.
- For Generic Host workers, include `builder.AddExceptionless(...)` plus `builder.UseExceptionless()` or explain how `ProcessQueueAsync()` is called during shutdown.
- For logging, explain the split between Microsoft logging rules and Exceptionless remote log-level settings.
- For advanced filtering, use server-synced settings or plugins instead of scattering ad hoc `if` statements around application code.
- For high-throughput logs, prefer in-memory storage for logging-only clients and document the durability tradeoff.
- For short-lived processes, serverless handlers, and CLI tools, flush with `await client.ProcessQueueAsync()` or an async-disposable scope before exit.

## Validation

Keep validation in the repository test suite, not in skill scripts. When editing this skill, verify the claims against current source/readmes and run the relevant `dotnet test` projects that cover the documented setup, configuration, logging, plugin, filtering, and privacy behavior.
