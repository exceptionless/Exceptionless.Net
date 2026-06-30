# Exceptionless MAUI Sample

This sample uses the core `Exceptionless` client from a .NET MAUI app. There is no MAUI-specific Exceptionless package, so the app registers an `ExceptionlessClient` in MAUI dependency injection and submits handled exceptions, log events, and feature-usage events from the main page.

## Configuration

The sample defaults to the same local development server used by the other samples:

- API key: `LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest`
- Server URL: `https://ex.dev.localhost:7111`

Override either value with environment variables before launch:

```bash
export EXCEPTIONLESS_API_KEY="YOUR_API_KEY"
export EXCEPTIONLESS_SERVER_URL="https://collector.exceptionless.io"
```

Events are queued under `FileSystem.Current.AppDataDirectory`, `IncludePrivateInformation` is disabled, and the sample has an explicit **Flush Queue** action. The app also asks the client to process the queue when the MAUI application goes to sleep.

## Run

Install the MAUI workload for the .NET SDK used by this repository, then run a target supported by your machine:

```bash
dotnet workload install maui
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-maccatalyst
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-ios
dotnet build samples/Exceptionless.SampleMaui/Exceptionless.SampleMaui.csproj -f net10.0-android
```

Android emulators cannot usually reach a host machine's `localhost` address directly. If you are sending to a local Exceptionless server from Android, use a URL that the emulator can reach and make sure the development certificate is trusted.
