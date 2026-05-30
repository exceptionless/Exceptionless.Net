# Exceptionless.Wpf Client


## When To Use

Use `Exceptionless.Wpf` for WPF desktop apps. It captures WPF/application-level unhandled exceptions and adds Windows environment information.

## Install

```powershell
Install-Package Exceptionless.Wpf
```

## Setup

App.config:

```xml
<exceptionless apiKey="VALID_API_KEY_12345" includePrivateInformation="false" />
```

Assembly attribute alternative:

```text
[assembly: Exceptionless.Configuration.Exceptionless("VALID_API_KEY_12345")]
```

Startup:

```text
Exceptionless.ExceptionlessClient.Default.Register()
```

## Sessions

Session tracking is useful for desktop analytics. Set user identity before session start when possible.

```text
ExceptionlessClient.Default.Configuration.UseSessions()
ExceptionlessClient.Default.Configuration.SetUserIdentity("UNIQUE_ID_OR_EMAIL_ADDRESS", "Display Name")
```

## Manual Handled Exceptions

```text
exception.ToExceptionless()
  .AddTags("wpf", "handled")
  .Submit()
```

## Best Practices

- Register in app startup, before UI event handlers can throw.
- Use folder or isolated storage for durable offline queues.
- Configure `IncludePrivateInformation` deliberately because desktop machine/user metadata can be sensitive.
- Flush with `ShutdownAsync` during clean application shutdown when possible.
