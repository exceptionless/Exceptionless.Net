# Exceptionless.WebApi Client


## When To Use

Use `Exceptionless.WebApi` for classic ASP.NET Web API applications. It registers Web API exception logging/filter behavior and supports adding `HttpActionContext` to manually submitted events.

## Install

```powershell
Install-Package Exceptionless.WebApi
```

## Startup Registration

Inside WebApiConfig registration:

```text
Exceptionless.ExceptionlessClient.Default.RegisterWebApi(config)
```

When Web API is hosted inside ASP.NET:

```text
Exceptionless.ExceptionlessClient.Default.RegisterWebApi(GlobalConfiguration.Configuration)
```

Configure the API key in web.config:

```xml
<exceptionless apiKey="VALID_API_KEY_12345" includePrivateInformation="false" />
```

## Manual Handled Exceptions

Web API does not have a static HTTP context. When possible, set the action context so request and user information are populated.

```text
exception.ToExceptionless()
  .SetHttpActionContext(ActionContext)
  .Submit()
```

## Configuration

Use web.config, app settings, attributes, or code before `RegisterWebApi`. For remote event filtering, use settings such as `@@error:Namespace.MyException=false` or `@@404:* = false`.

## Best Practices

- Register once during application startup.
- Use `SetHttpActionContext` for handled exceptions and custom events inside controllers.
- Configure data exclusions for headers, query strings, cookies, and form data.
- Validate that Web API and ASP.NET-level exception handlers are not double-submitting.
