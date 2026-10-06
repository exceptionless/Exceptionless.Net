# Modern DI and System.Text.Json implementation plan

## Keyed activation correction — October 5, 2026

The `[ServiceKey]` blocker is fixed. Supplied implementation-type descriptors now remain native Microsoft DI descriptors; the resolver no longer rewrites them into `ActivatorUtilities` factories. This preserves native constructor selection, keyed binding, and open-generic provider injection, and removes the blanket open-generic rejection. Legacy resolver type registrations still use their existing compatibility activation path. Caller factories keep cycle and disposable-identity guards, with a resolution lease around the factory invocation.

All 55 focused DI tests pass, including in the complete post-rebase suite. Coverage includes singleton/scoped/transient key binding with a second ordinary string dependency and injected provider, valid different-key and keyed-to-unkeyed composition, same-key cycles, cross-thread singleton cycles, open generics, registration replacement, and disposal behavior. Native providers captured by supplied implementation constructors follow Microsoft's normal provider/scope lifetime contract: callers must finish using them and dispose their scopes before disposing the client. Resolver and factory calls already in progress are protected by resolution leases. This explicit ownership boundary preserves standard DI behavior without adding a constructor-selection engine.

The branch is rebased onto `main` at `2054b80`, retaining its SDK 10.0.401, SourceLink 10.0.401, MinVer 8, workflow security settings, and newer test packages. Rebase integration corrections are limited to the coverage package references, the HTTP test's explicit .NET Framework assembly reference, and a framework-compatible cancellation-aware timeout overload.

## Current validation — October 5, 2026

- The complete non-Windows suite passes: 433 core and 15 MessagePack tests, zero failures, and 18 existing skips. Fresh complete runs after the test-only compatibility and ownership-test corrections produced the same result.
- Cobertura coverage from the final passing run is 74.29% line / 51.12% branch overall; the core product assembly is 76.67% / 52.02%. The DI resolver is 91.30% / 77.14%, its fallback provider is 88.88% / 77.27%, `JsonValueWriter` is 92.01% / 86.58%, and `DataDictionaryConverter` is 92.59% / 81.25%. Coverage is not a claim that every possible edge case is proven.
- The complete Windows-shaped Release solution builds with zero warnings and errors, including `net462` production assets and `net472` tests. Runtime execution requires Windows CI.
- A run-unique local NuGet package passes the consumer smoke using its actual `netstandard2.0` asset. This test intentionally compiles for net7 and rolls forward to an installed modern runtime, so STJ 10 dependencies emit expected unsupported-TFM build warnings; it does not establish support for executing on .NET 7.
- The reflection-disabled `net10.0` smoke passes against the packed package. Real package/hosting NativeAOT publication and execution for both modern targets remain exact-head Linux CI gates.
- SDK API comparisons against released 6.2.0 pass for `netstandard2.0`, `net8.0`, `net10.0`, and `net462`. Whole-package validation separately reports three existing cross-framework differences in `CertificateData` and `ReadFromConfiguration`; no suppression or public API change was introduced to hide those differences.
- One initial Windows CI run exposed a stale concurrency expectation: a native constructor-injected provider was still expected to hold the resolver's lease during concurrent disposal. The regression now explicitly verifies native provider/scope ownership, while concurrency tests retain the leased factory provider and obtain leasing scopes through the resolver. No test skip, retry, or longer timeout was added to conceal the mismatch.
- The current direct/transitive vulnerability audit is clean across all 15 Windows-solution projects. Whitespace checks pass, and the largest changed C# file is 849 lines.
- The final published revision must pass Linux, macOS, Windows, and CLA before merge. Keep the PR draft pending maintainer approval of the 7.0 release impact below; local results alone are not merge approval.

## Historical merge review — September 21, 2026

The restored snapshot exposed two false keyed-service cycle failures and loss of `[ServiceKey]` constructor binding. Including the service key in activation identity corrected the false cycles; preserving native implementation descriptors corrected the constructor regression in the October review. The earlier 427-core/15-MessagePack passing results had three failures and were not merge approval. SourceLink's earlier vulnerability-related restore failure is also superseded by the patched tooling retained from current `main`.

## Compatibility baseline

1. Run the current non-Windows solution tests before editing.
2. Inventory exact JSON assertions, storage round-trips, resolver semantics, hosting registrations, and package target frameworks.

## System.Text.Json

1. Rebase the existing `niemyjski/drop-json-net-use-stj` commits onto current `main`.
2. Resolve package-version and current-main conflicts without weakening serializer tests.
3. Retain exact collector property names, enum strings, null/default output, `DataDictionary` raw-value markers, settings coercion, POST data behavior, exclusions, maximum depth, and stream ownership.
4. Run focused serializer, storage, configuration exclusion, submission, and MessagePack tests.

## Dependency injection

1. Add Microsoft.Extensions.DependencyInjection to the core package for all supported targets.
2. Replace TinyIoC in `DefaultDependencyResolver` with an `IServiceCollection`-backed provider.
3. Preserve the existing registration lifetimes: interface/abstract mappings are singletons, concrete mappings and factories are transient, and explicit instances remain externally owned singletons.
4. Preserve isolated resolvers, constructor injection, singleton resolution, unregistered concrete activation, null argument behavior, disposal, and registrations made after an initial resolution.
5. Add a constructor/customization seam that accepts an `IServiceCollection` so new code can use normal Microsoft DI registrations without depending on the legacy methods.
6. Add focused tests for service collection customization, replacement registration, late registration, and disposable singleton ownership.

## Validation and readiness

1. Build the non-Windows solution with warnings as errors.
2. Run the complete non-Windows test suite.
3. Pack the core and modern platform projects to catch dependency/package metadata issues.
4. Inspect the final diff for accidental API breaks and remaining Newtonsoft/TinyIoC references.
5. Document Windows-only validation that still needs CI when it cannot be run on macOS.

## NativeAOT hardening

1. Multi-target the core package for `net8.0` and `net10.0` and enable the built-in AOT, trimming, and single-file analyzers.
2. Use an Exceptionless-owned `JsonSerializerContext` for the wire model and accept a consumer resolver/context for arbitrary event data.
3. Require NativeAOT consumers to register services and custom payload metadata explicitly; retain reflection and unregistered concrete activation only for dynamic-code runtimes.
4. Use regular runtime stack traces on modern targets instead of the IL/PDB demystifier, while keeping exception capture and serialization functional with reduced metadata.
5. Publish and execute a warning-as-error NativeAOT smoke application for both `net8.0` and `net10.0`, covering default Microsoft DI/default serialization, custom STJ metadata, storage, queues, submission, and nested exception capture in Linux CI.

## Original execution results (historical)

- Baseline: 300 core tests and 10 MessagePack tests passed; 18 existing tests were skipped.
- Final non-Windows suite: 377 core tests and 13 MessagePack tests passed; 0 failed; 18 existing tests were skipped.
- Focused DI coverage now includes singleton and transient compatibility, constructor injection, isolated containers, coherent provider snapshots after late replacement, `IServiceCollection` overrides and enumeration, open generics, deterministic constructor/factory cycle detection without false positives across concurrent provider resolutions, captured `IServiceProvider` behavior, exact disposal of every provider snapshot, external instance ownership, per-host client isolation, and host-versus-caller disposal ownership.
- Serializer coverage retains exact wire-format assertions and adds regressions for exclusions inside collections and settings, named floating-point values, declared-type depth limits, cycles, malformed getters, predicates, converters, and iterators, initialized source-generated contexts, STJ custom converters/number handling/extension data, stream/string parity, public fields, the legacy ignore attribute, every `DataDictionary` interface path, structured/raw values, failed-object diagnostics, null/unstructured POST data, settings coercion, and MessagePack storage round-trips.
- Linux CI now collects and uploads Cobertura coverage for both test assemblies. The final core report is 74.2% line / 50.1% branch overall and 88.9% of executable production lines changed by the PR are covered; the migration-critical components are substantially higher: `JsonValueWriter` 97.2% line / 92.5% branch, `DataDictionaryConverter` 92.6% / 81.3%, `JsonElementValueConverter` 95.7% / 95.7%, `PostDataConverter` 95.8% / 72.2%, `SettingsDictionaryConverter` 88.9% / 83.3%, `DataDictionary` 98.9% / 92.0%, and the Microsoft DI resolver/fallback 85.8% / 64.5% and 89.5% / 80.0% respectively.
- `net462` core and `net472` test assemblies build with 0 warnings and 0 errors when Windows targeting is forced on macOS. Windows CI executes the `net472` suite on real .NET Framework and consumes the packed `net462` asset in a separate smoke process.
- The non-Windows solution packs successfully, with the root Markdown documentation declared as the modern NuGet package readme while retaining the legacy `readme.txt`. A Windows-shaped core package containing both `net462` and `netstandard2.0` assets also packs successfully with explicit `SolutionDir`.
- Packed-package consumer smokes now verify the shipped compatibility assets instead of relying only on project references: Linux runs a `net7.0` consumer with major runtime roll-forward so NuGet must select `netstandard2.0`, while Windows runs a `net472` consumer so NuGet must select `net462`. Both assert the loaded assembly's target-framework metadata, an `IServiceCollection` registration, JSON serialization/deserialization, and a storage-stream round-trip. Each workflow uses a run-unique package version from an ephemeral runner-local source, preventing a public feed or global package cache from satisfying the smoke accidentally.
- `net8.0` and `net10.0` core builds pass with 0 AOT/trim/single-file warnings. Real NativeAOT executables now cover the packed NuGet assets, default Microsoft DI/default serialization, built-in configuration deserialization, source-generated custom properties and fields, compatibility-ignore filtering, event-batch serialization, storage round-trips, queued submission, identifiable exception frames across rethrow boundaries, and Generic Host startup/shutdown on both target frameworks. The `net10.0` smoke found and fixed an offset-only runtime frame with no usable identity; those empty frames are now omitted. Linux CI publishes and executes the package and hosting smoke applications with ILLink and trim-analysis warnings treated as errors.
- SDK ApiCompat is clean from the previous `netstandard2.0` asset to both the new `netstandard2.0` and `net8.0` assets. Compatibility shims retain the legacy ignore attribute, serializer delegates, and `AssemblyHelper.GetTypes` API.
- The current direct and transitive NuGet vulnerability audit is clean across every production and test project.
- Release builds now actually treat warnings as errors; the previous `WarningsAsErrors=true` property was a warning-code list rather than the boolean build gate.

## Release impact

- The non-Windows core package grows from 596,508 bytes to 699,874 bytes (about 17%) because it contains `netstandard2.0`, `net8.0`, and `net10.0` assets plus the modern package readme instead of one asset. Its uncompressed contents shrink from 2,261,763 bytes to 1,824,641 bytes (about 19%), and the `netstandard2.0` assembly shrinks from 998,912 bytes to 354,304 bytes (about 65%).
- Every package inherits `Microsoft.Extensions.DependencyInjection` 8 through the core package. The `netstandard2.0`, `net462`, and `net8.0` dependency groups also carry System.Text.Json 10, which is the largest deployment and binding-risk change for .NET Framework applications.
- The existing `IDependencyResolver` and public setup APIs remain compatible. Microsoft DI backs the client-owned resolver and exposes an `IServiceCollection` customization seam; registrations from an application's root Generic Host provider are not automatically imported into the client's isolated provider.
- Configuration-based hosting overloads now create one client owned by each host provider instead of mutating the process-wide `ExceptionlessClient.Default`. Explicit client overloads remain caller-owned. Configuration-based logging providers likewise own an isolated client, while providers given an existing client only flush it and do not dispose it. This removes cross-host configuration/disposal interference but changes code that intentionally depended on those overloads mutating the global default.
- Custom payloads now follow System.Text.Json contracts. Public fields and `ExceptionlessIgnoreAttribute` remain supported, but consumers relying on Newtonsoft-specific contracts such as `DataContract`/`DataMember` behavior should migrate to STJ attributes and source-generated metadata.
- Filtered event serialization remains streaming for normal objects, dictionaries, and collections. Converter-backed leaf values are preflighted before their property name is written so a failing converter cannot corrupt the containing event; this adds a small per-leaf allocation only for custom converter-backed values.
- The `net8.0` and later core assets use normal runtime stack traces instead of the legacy IL/PDB demystifier. NativeAOT frames retain parsed method identity, but modern JIT consumers also lose enhanced demystification.
- Although ApiCompat is clean, the serializer behavior, transitive dependency graph, target assets, and modern-stack-trace change are substantial enough to recommend a 7.0 release rather than shipping the work as 6.2.1.

## Remaining release gates

- Require the exact-head Linux `netstandard2.0` and Windows `net462` packed-package consumer smokes to remain green alongside the existing platform test matrix.
- Require the distinct `Linux`, `macOS`, and `Windows` jobs in branch protection. Their check names are now unambiguous, but the repository currently requires only `license/cla`, so the regression gates are not merge-enforced yet.
- Keep draft PR #368 open for maintainer approval of the 7.0 compatibility and ownership changes. Pull-request package validation remains ephemeral; non-PR Windows pushes retain the repository's existing prerelease publishing behavior.

## Historical NativeAOT comparison

The same strict NativeAOT client/serialization/storage probe was published and executed against four repository states:

| Commit | State | NativeAOT result |
| --- | --- | --- |
| `5eb567a` | Vendored Newtonsoft + TinyIoC | Published with 10 TinyIoC AOT/trim warnings, then exited 134 because TinyIoC could not construct `DefaultJsonSerializer`. |
| `2dd1d8e` | System.Text.Json + TinyIoC | Same TinyIoC warnings and runtime construction failure. |
| `118123b` | System.Text.Json + Microsoft DI before AOT hardening | Published with 4 DI trim warnings, then exited 134 because the serializer constructor was trimmed. |
| `ef0088b` | Hardened System.Text.Json + Microsoft DI | Published with no product AOT/trim warnings and exited 0 with `AOT_HISTORY_PROBE_OK`. |

The legacy Newtonsoft serializer was also instantiated directly to remove TinyIoC as a confound. Under NativeAOT it serialized a normal POCO as `{}` and then failed built-in `Event` storage serialization because `SnakeCaseNamingStrategy` construction metadata had been trimmed. The pre-modernization client therefore had two independent NativeAOT blockers: TinyIoC and reflection-driven Newtonsoft serialization.
