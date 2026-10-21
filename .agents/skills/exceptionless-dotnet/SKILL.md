---
name: exceptionless-dotnet
description: Integrate, configure, document, review, or troubleshoot Exceptionless .NET clients using the appropriate platform package and current repository APIs.
---

# Exceptionless .NET

Prefer current repository source and package readmes over older public docs when they differ. Within the requested scope, choose the approach that best serves app users, developers, and agents together; preserve existing behavior and contracts except where the requested change requires otherwise.

## Choose References

Identify the host and package involved, then read only the references relevant to the request:

- `references/client-package-selection.md` for package choice, source locations, and public docs.
- `references/client-console-services.md` for console apps, simple services, Blazor WebAssembly, and serverless functions.
- `references/client-aspnetcore.md` for ASP.NET Core setup and middleware.
- `references/client-extensions-hosting.md` for Generic Host setup and lifecycle.
- `references/client-extensions-logging.md` for Microsoft `ILogger` integration.
- `references/client-nlog.md` and `references/client-log4net.md` for those logging integrations.
- `references/client-mvc.md`, `references/client-webapi.md`, and `references/client-web-webforms-wcf.md` for legacy ASP.NET packages.
- `references/client-wpf.md` and `references/client-windows-forms.md` for desktop packages.
- `references/client-messagepack.md` for queue serialization.
- `references/client-configuration-runtime-settings.md` for API keys, config sources, self-hosted URLs, storage, server-synced settings, and sessions.
- `references/client-plugins-event-pipeline.md` for plugins, priorities, cancellation, enrichment, and event exclusions.
- `references/client-log-levels-filtering.md` for remote log filters, type/source filters, and high-throughput logging.
- `references/client-privacy-data-exclusions-troubleshooting.md` for privacy, diagnostics, proxies, de-duplication, and upgrades.

## Integration Constraints

- Prefer the platform package when it provides richer context and lifecycle handling than the root client. Use dependency injection for ASP.NET Core and hosted services; use `ExceptionlessClient.Default` when DI is not available.
- Configure before the first event is submitted. API key and server URLs lock after queue/submission startup.
- For production examples, explicitly choose `IncludePrivateInformation`, data exclusions, and any plugin redaction; default collection can include private metadata.
- Use server-synced settings or plugins for advanced filtering. Distinguish Microsoft logging rules from Exceptionless remote log-level settings.
- For high-throughput logging-only clients, prefer in-memory storage and explain the durability tradeoff.
- For short-lived processes and serverless handlers, await `ProcessQueueAsync()` or use an async-disposable flush scope before exit. For hosted apps, follow the package's shutdown lifecycle.
- Complete setup recommendations should identify the package, startup call, configuration source, queue-flush behavior, and privacy stance. A narrow question needs only the relevant details.

## Validation

Verify changed technical claims against current source and package readmes. For changes to examples or documented behavior, run the existing repository tests that exercise the affected API; add coverage only for a meaningful gap. Wording-only edits need metadata and reference checks, not unrelated application tests.

Keep executable validation in the repository test suite. Do not add scripts, generated validators, or other runnable code inside this skill.
