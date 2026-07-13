---
date: 2026-07-12
topic: modern-di-stj
---

# Modern DI and System.Text.Json

## What We're Building

Modernize the Exceptionless .NET client without requiring consumers to rewrite their existing setup. Replace the vendored internal Newtonsoft.Json fork with System.Text.Json while preserving the collector wire format, persisted queue compatibility, exclusions, depth limits, and arbitrary `DataDictionary` values. Replace TinyIoC with Microsoft.Extensions.DependencyInjection behind the existing `IDependencyResolver`, and add an `IServiceCollection` customization seam for new integrations.

## Why This Approach

A direct removal of `IDependencyResolver` would break configuration extensions, plugins, legacy platforms, isolated clients, and applications that do not use the Generic Host. Keeping TinyIoC indefinitely avoids that break but leaves the client with a private container implementation and prevents normal Microsoft DI customization. The staged adapter approach modernizes the implementation now, makes Microsoft DI the extensibility path, and leaves removal of the compatibility facade for a future major release.

The STJ migration already exists as four focused commits on `niemyjski/drop-json-net-use-stj`. Reusing and rebasing that work is lower-risk than independently recreating its converters and regression fixes.

## Key Decisions

- Preserve `IDependencyResolver` and all existing `ExceptionlessClient` constructors in this release.
- Back `DefaultDependencyResolver` with `Microsoft.Extensions.DependencyInjection` singleton registrations.
- Preserve unregistered concrete-type activation and replacement registration behavior.
- Allow explicit `IServiceCollection` customization before the resolver is used.
- Keep Exceptionless' internally owned provider isolated from an application's root provider; hosted apps continue resolving `ExceptionlessClient` from application DI.
- Treat serialized JSON as a compatibility contract, not merely valid JSON.
- Keep reflection fallback for `netstandard2.0` and `net462`, but use built-in source-generated metadata for the modern `net8.0` and `net10.0` assets. NativeAOT consumers register an additional `JsonSerializerContext` for their own payload types.

## Open Questions

- Removing `IDependencyResolver` and making every internal service directly application-DI-owned is deferred to a future major version.
- NativeAOT deliberately uses ordinary runtime stack traces instead of the bundled IL/PDB demystifier because the latter depends on metadata that trimming removes.

## Next Steps

Implement the plan in `docs/plans/2026-07-12-modern-di-stj-plan.md` and verify focused serializer, resolver, storage, hosting, and full non-Windows test coverage.
