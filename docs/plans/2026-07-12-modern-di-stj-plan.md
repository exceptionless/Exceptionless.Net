# Modern DI and System.Text.Json implementation plan

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

## Execution results

- Baseline: 300 core tests and 10 MessagePack tests passed; 18 existing tests were skipped.
- Final non-Windows suite: 370 core tests and 12 MessagePack tests passed; 0 failed; 18 existing tests were skipped.
- Focused DI coverage now includes singleton and transient compatibility, constructor injection, isolated containers, coherent provider snapshots after late replacement, `IServiceCollection` overrides and enumeration, open generics, exact disposal of every provider snapshot, external instance ownership, per-host client isolation, and host-versus-caller disposal ownership.
- Serializer coverage retains exact wire-format assertions and adds regressions for exclusions inside collections and settings, named floating-point values, declared-type depth limits, cycles, malformed getters, predicates, converters, and iterators, STJ custom converters/number handling/extension data, stream/string parity, public fields, the legacy ignore attribute, every `DataDictionary` interface path, structured/raw values, settings and POST-data coercion, and MessagePack raw-object round-trips.
- Linux CI now collects and uploads Cobertura coverage for both test assemblies. The final core report is 74% line / 50% branch overall and 88.1% of executable production lines changed by the PR are covered; the migration-critical components are substantially higher: `JsonValueWriter` 97% line / 93% branch, `DataDictionaryConverter` 93% / 81%, `JsonElementValueConverter` 96% / 96%, `SettingsDictionaryConverter` 89% / 83%, `DataDictionary` 99% / 92%, and the Microsoft DI resolver 84% / 61%.
- `net462` core and `net472` test assemblies build with 0 warnings and 0 errors when Windows targeting is forced on macOS. Runtime execution of the .NET Framework tests still belongs in Windows CI.
- The non-Windows solution packs successfully. A Windows-shaped core package containing both `net462` and `netstandard2.0` assets also packs successfully with explicit `SolutionDir`.
- `net8.0` and `net10.0` core builds pass with 0 AOT/trim/single-file warnings. Real NativeAOT executables now cover the packed NuGet assets, default Microsoft DI/default serialization, source-generated custom properties and fields, event-batch serialization, storage round-trips, queued submission, identifiable exception frames across rethrow boundaries, and Generic Host startup/shutdown on both target frameworks. The `net10.0` smoke found and fixed an offset-only runtime frame with no usable identity; those empty frames are now omitted. Linux CI publishes and executes the package and hosting smoke applications with linker warnings treated as errors.
- SDK ApiCompat is clean from the previous `netstandard2.0` asset to both the new `netstandard2.0` and `net8.0` assets. Compatibility shims retain the legacy ignore attribute, serializer delegates, and `AssemblyHelper.GetTypes` API.
- Release builds now actually treat warnings as errors; the previous `WarningsAsErrors=true` property was a warning-code list rather than the boolean build gate.

## Release impact

- The non-Windows core package grows from 596,508 bytes to 686,321 bytes (about 15%) because it contains `netstandard2.0`, `net8.0`, and `net10.0` assets instead of one asset. Its uncompressed contents shrink from 2,261,763 bytes to 1,792,690 bytes (about 21%), and the `netstandard2.0` assembly shrinks from 998,912 bytes to 348,160 bytes (about 65%).
- Every package inherits `Microsoft.Extensions.DependencyInjection` 8 through the core package. The `netstandard2.0`, `net462`, and `net8.0` dependency groups also carry System.Text.Json 10, which is the largest deployment and binding-risk change for .NET Framework applications.
- The existing `IDependencyResolver` and public setup APIs remain compatible. Microsoft DI backs the client-owned resolver and exposes an `IServiceCollection` customization seam; registrations from an application's root Generic Host provider are not automatically imported into the client's isolated provider.
- Configuration-based hosting overloads now create one client owned by each host provider instead of mutating the process-wide `ExceptionlessClient.Default`. Explicit client overloads remain caller-owned. Configuration-based logging providers likewise own an isolated client, while providers given an existing client only flush it and do not dispose it. This removes cross-host configuration/disposal interference but changes code that intentionally depended on those overloads mutating the global default.
- Custom payloads now follow System.Text.Json contracts. Public fields and `ExceptionlessIgnoreAttribute` remain supported, but consumers relying on Newtonsoft-specific contracts such as `DataContract`/`DataMember` behavior should migrate to STJ attributes and source-generated metadata.
- Filtered event serialization remains streaming for normal objects, dictionaries, and collections. Converter-backed leaf values are preflighted before their property name is written so a failing converter cannot corrupt the containing event; this adds a small per-leaf allocation only for custom converter-backed values.
- The `net8.0` and later core assets use normal runtime stack traces instead of the legacy IL/PDB demystifier. NativeAOT frames retain parsed method identity, but modern JIT consumers also lose enhanced demystification.
- Although ApiCompat is clean, the serializer behavior, transitive dependency graph, target assets, and modern-stack-trace change are substantial enough to recommend a 7.0 release rather than shipping the work as 6.2.1.

## Remaining release gates

- Run the net462/net472 tests and package-load checks on a real Windows runner; macOS cross-compilation passes but cannot prove .NET Framework runtime binding behavior.
- Require the Linux, macOS, and Windows build jobs in branch protection. The repository currently requires only `license/cla`, so the new regression gates are not merge-enforced yet.
- Keep draft PR #368 open until the real Windows runtime checks are green. Be aware that additional non-PR Windows pushes publish prerelease packages to GitHub Packages and Feedz.

## Historical NativeAOT comparison

The same strict NativeAOT client/serialization/storage probe was published and executed against four repository states:

| Commit | State | NativeAOT result |
| --- | --- | --- |
| `5eb567a` | Vendored Newtonsoft + TinyIoC | Published with 10 TinyIoC AOT/trim warnings, then exited 134 because TinyIoC could not construct `DefaultJsonSerializer`. |
| `2dd1d8e` | System.Text.Json + TinyIoC | Same TinyIoC warnings and runtime construction failure. |
| `118123b` | System.Text.Json + Microsoft DI before AOT hardening | Published with 4 DI trim warnings, then exited 134 because the serializer constructor was trimmed. |
| `ef0088b` | Hardened System.Text.Json + Microsoft DI | Published with no product AOT/trim warnings and exited 0 with `AOT_HISTORY_PROBE_OK`. |

The legacy Newtonsoft serializer was also instantiated directly to remove TinyIoC as a confound. Under NativeAOT it serialized a normal POCO as `{}` and then failed built-in `Event` storage serialization because `SnakeCaseNamingStrategy` construction metadata had been trimmed. The pre-modernization client therefore had two independent NativeAOT blockers: TinyIoC and reflection-driven Newtonsoft serialization.
