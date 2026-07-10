# Exceptionless MAUI Sample

This sample uses the core `Exceptionless` client from a .NET MAUI app. There is no MAUI-specific Exceptionless package, so the app registers an `ExceptionlessClient` in MAUI dependency injection and submits handled exceptions, log events, and feature-usage events from the main page.

## Configuration

The sample defaults to the local development server used by the mobile samples:

- API key: `LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest`
- iOS, Mac Catalyst, and Windows server URL: `http://localhost:7110`
- Android emulator server URL: `http://10.0.2.2:7110`

Override either value with environment variables before launch:

```bash
export EXCEPTIONLESS_API_KEY="YOUR_API_KEY"
export EXCEPTIONLESS_SERVER_URL="https://collector.exceptionless.io"
```

Events are queued under `FileSystem.Current.AppDataDirectory`, `IncludePrivateInformation` is disabled, and the sample has an explicit **Flush Queue** action. The app also asks the client to process the queue when the MAUI application goes to sleep.

Use **Refresh Config** to force a project configuration fetch. The page shows the `SampleMaui.ConfigValue` server setting after it is loaded.

For command-line dogfooding, set `EXCEPTIONLESS_SAMPLE_AUTORUN=true` and optionally `EXCEPTIONLESS_SAMPLE_AUTORUN_RESULT_PATH` before launching the app. Autorun refreshes project configuration, submits a handled exception, submits a warning log, tracks feature usage, flushes the queue, and writes a small result file when a result path is supplied.

## Build And Run

Install the MAUI workload for the .NET SDK used by this repository, then build a target supported by your machine:

```bash
dotnet workload install maui
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-maccatalyst
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-ios
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-android
```

Launch the Mac Catalyst target from the command line with:

```bash
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -t:Run -f net10.0-maccatalyst
```

Physical Android devices cannot reach the host machine through `10.0.2.2`. Set `EXCEPTIONLESS_SERVER_URL` to an HTTP URL containing the development machine's LAN address when running on a physical device.
