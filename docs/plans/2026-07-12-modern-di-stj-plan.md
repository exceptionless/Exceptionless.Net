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

## Execution results

- Baseline: 300 core tests and 10 MessagePack tests passed; 18 existing tests were skipped.
- Final non-Windows suite: 313 core tests and 10 MessagePack tests passed; 0 failed; the same 18 tests were skipped.
- Focused DI coverage now includes singleton and transient compatibility, constructor injection, isolated containers, late replacement, `IServiceCollection` overrides, open generics, container disposal, external instance ownership, and host-provider disposal of the default client.
- Serializer coverage retains exact wire-format assertions and adds regressions for exclusions, depth limits, `DataDictionary` structured/raw values, settings coercion, and MessagePack marker round-trips.
- `net462` core and `net472` test assemblies build with 0 warnings and 0 errors when Windows targeting is forced on macOS. Runtime execution of the .NET Framework tests still belongs in Windows CI.
- The non-Windows solution packs successfully. A Windows-shaped core package containing both `net462` and `netstandard2.0` assets also packs successfully with explicit `SolutionDir`.
