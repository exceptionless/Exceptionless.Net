# Exceptionless.Log4net Appender


## When To Use

Use `Exceptionless.Log4net` when an application already uses log4net and should send selected log4net events to Exceptionless. The appender can use the root Exceptionless config section or an API key configured directly on the appender.

## Install

```bash
dotnet add package Exceptionless.Log4net
```

## Minimal Appender

```xml
<appender name="exceptionless" type="Exceptionless.Log4net.ExceptionlessAppender, Exceptionless.Log4net" />
```

## Appender With API Key

```xml
<appender name="exceptionless" type="Exceptionless.Log4net.ExceptionlessAppender, Exceptionless.Log4net">
  <apiKey value="VALID_API_KEY_12345" />
</appender>
```

## Root Configuration

When the API key is not on the appender, configure the root Exceptionless section:

```xml
<configuration>
  <configSections>
    <section name="exceptionless" type="Exceptionless.ExceptionlessSection, Exceptionless" />
  </configSections>
  <exceptionless apiKey="VALID_API_KEY_12345" includePrivateInformation="false" />
</configuration>
```

## Log Level Strategy

log4net filters first, Exceptionless filters second. Use log4net thresholds to keep obvious noise out of the appender and Exceptionless `@@log:` settings for remote operational tuning.

## Best Practices

- Do not duplicate Exceptionless appenders across inherited log4net configuration files.
- Keep API keys in environment-specific config.
- Enable Exceptionless client diagnostics when appender events do not appear.
- Use data exclusions and redaction plugins for sensitive log properties.
