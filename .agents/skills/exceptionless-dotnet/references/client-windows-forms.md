# Exceptionless.Windows WinForms Client


## When To Use

Use `Exceptionless.Windows` for Windows Forms applications. It wires Windows Forms/unhandled exception capture and Windows environment information.

## Install

```powershell
Install-Package Exceptionless.Windows
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

```text
ExceptionlessClient.Default.Configuration.UseSessions()
ExceptionlessClient.Default.Configuration.SetUserIdentity("UNIQUE_ID_OR_EMAIL_ADDRESS", "Display Name")
```

## Manual Handled Exceptions

```text
exception.ToExceptionless()
  .AddTags("winforms", "handled")
  .Submit()
```

## Best Practices

- Register before showing the main form.
- Configure durable queue storage for offline desktop users.
- Avoid collecting machine/user/IP information unless operationally needed and permitted.
- Flush on graceful exit when the application has a controlled shutdown path.
