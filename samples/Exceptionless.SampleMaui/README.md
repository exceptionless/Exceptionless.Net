# Exceptionless MAUI Sample

This sample uses the core `Exceptionless` client from a .NET MAUI app. It configures `ExceptionlessClient.Default`, registers that same instance in MAUI dependency injection, and submits handled exceptions, log events, and feature-usage events from the main page.

## Configuration

Set your project's API key before launch. The sample uses the SDK's default hosted services unless you explicitly configure a self-hosted or development server:

```bash
export Exceptionless__ApiKey="YOUR_API_KEY"
# Optional, for a self-hosted or development server:
# export Exceptionless__ServerUrl="YOUR_SERVER_URL"
```

For device launches without environment variables, load the API key from your app's configuration or secure storage and assign it to `ExceptionlessClient.Default.Configuration.ApiKey` before `Startup()`. Keep project keys out of committed source. The sample has no embedded API key or development address. The SDK reads its standard environment variables during `Startup()`; no sample-specific configuration adapter is needed. Invalid configuration or a disabled client disables the page actions and makes autorun report failure.

Events are queued under `FileSystem.Current.AppDataDirectory`, `IncludePrivateInformation` is disabled, and the sample has an explicit **Flush Queue** action. The app also asks the client to process the queue when the MAUI application goes to sleep. The SDK still attaches a persistent installation ID; the shared Apple privacy manifest declares that identifier and the linked diagnostic/usage events without advertising tracking.

The client's default duplicate checker can delay repeated identical events for up to a minute and combine their occurrence counts. **Flush Queue** processes events already in the queue; it does not bypass duplicate checking.

Use **Refresh Config** to force a project configuration fetch. The page shows the `SampleMaui.ConfigValue` server setting after it is loaded.

For command-line dogfooding, set `EXCEPTIONLESS_SAMPLE_AUTORUN=true` and optionally `EXCEPTIONLESS_SAMPLE_AUTORUN_RESULT_PATH` before launching the app. Autorun refreshes project configuration, submits a handled exception, submits a warning log, tracks feature usage, flushes the queue, and writes a small result file when a result path is supplied.

## Build And Run

Install the MAUI workload for the .NET SDK used by this repository, then build a target supported by your machine:

```bash
dotnet workload install maui --version 10.0.401
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-maccatalyst
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-ios
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-android
```

Launch the Mac Catalyst target from the command line with:

```bash
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -t:Run -f net10.0-maccatalyst
```

For local development, set `Exceptionless__ServerUrl` to an address reachable from the target device or emulator. Android emulators require the host alias described in the [MAUI local web services guidance](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/local-web-services?view=net-maui-10.0); physical devices require a reachable host address. Prefer HTTPS for remote servers. The iOS sample permits local HTTP connections through `NSAllowsLocalNetworking`.
