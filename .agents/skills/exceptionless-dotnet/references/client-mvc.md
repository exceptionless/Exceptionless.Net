# Exceptionless.Mvc Client


## When To Use

Use `Exceptionless.Mvc` for classic ASP.NET MVC applications. The NuGet package configures web.config so unhandled MVC errors are captured through the web module/filter integration.

## Install

```powershell
Install-Package Exceptionless.Mvc
```

## Setup

After installing, set the API key in web.config:

```xml
<configuration>
  <configSections>
    <section name="exceptionless" type="Exceptionless.ExceptionlessSection, Exceptionless" />
  </configSections>
  <exceptionless apiKey="VALID_API_KEY_12345" includePrivateInformation="false" />
</configuration>
```

## Manual Handled Exceptions

```text
try {
    throw new InvalidOperationException("Handled MVC exception.");
} catch (Exception ex) {
    ex.ToExceptionless()
      .AddTags("mvc", "handled")
      .Submit();
}
```

## Configuration

Classic MVC supports the root config section, app settings, assembly attributes, and code configuration before startup/module initialization. Use the cross-cutting configuration reference for storage, logging, default tags/data, data exclusions, and settings.

## Best Practices

- Prefer `Exceptionless.Mvc` over the root package for classic MVC because it wires web-specific capture and context.
- Explicitly configure privacy and data exclusions in web.config.
- Avoid submitting the same exception from both MVC filters and manual catch blocks unless duplicate submissions are intended.
- Keep API keys out of committed transforms where possible.
