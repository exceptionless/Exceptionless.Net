# Exceptionless.Web WebForms And WCF Client


## When To Use

Use `Exceptionless.Web` for ASP.NET WebForms and WCF scenarios. It installs web.config integration for ASP.NET unhandled errors and exposes WCF handling attributes.

## Install

```powershell
Install-Package Exceptionless.Web
```

## WebForms Setup

Configure web.config:

```xml
<configuration>
  <configSections>
    <section name="exceptionless" type="Exceptionless.ExceptionlessSection, Exceptionless" />
  </configSections>
  <exceptionless apiKey="VALID_API_KEY_12345" includePrivateInformation="false" />
</configuration>
```

The package module calls startup and registers the ASP.NET application error handler.

## WCF Setup

Add the WCF handler attribute to WCF service classes:

```text
[ExceptionlessWcfHandleError]
public class Service1 : IService1 {
}
```

## Manual Handled Exceptions

```text
exception.ToExceptionless()
  .AddTags("webforms", "handled")
  .Submit()
```

## Best Practices

- Use web.config transforms for environment-specific API keys and server URLs.
- Turn off private information or specific request data collection when handling sensitive web traffic.
- Add user identity when authenticated user context is meaningful and permitted.
- Enable client diagnostics temporarily when module startup or WCF capture fails.
