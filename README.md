# Exceptionless .NET Clients

[![Build Windows](https://github.com/Exceptionless/Exceptionless.Net/actions/workflows/build-windows.yml/badge.svg?branch=main)](https://github.com/Exceptionless/Exceptionless.Net/actions/workflows/build-windows.yml)
[![Build OSX](https://github.com/Exceptionless/Exceptionless.Net/actions/workflows/build-osx.yml/badge.svg?branch=main)](https://github.com/Exceptionless/Exceptionless.Net/actions/workflows/build-osx.yml)
[![Build Linux](https://github.com/Exceptionless/Exceptionless.Net/actions/workflows/build-linux.yml/badge.svg?branch=main)](https://github.com/Exceptionless/Exceptionless.Net/actions/workflows/build-linux.yml)
[![NuGet Version](http://img.shields.io/nuget/v/Exceptionless.svg?style=flat)](https://www.nuget.org/packages/Exceptionless/)
[![Discord](https://img.shields.io/discord/715744504891703319)](https://discord.gg/6HxgFCx)

The definition of the word exceptionless is: to be without exception. [Exceptionless](https://exceptionless.com/) provides real-time .NET error reporting for your ASP.NET, Web API, WebForms, WPF, Console, and MVC apps. It organizes the gathered information into simple actionable data that will help your app become exceptionless!

## Using Exceptionless

Refer to the Exceptionless documentation here: [Exceptionless Docs](https://exceptionless.com/docs/).

### Deployment environments

Every event can carry a deployment environment, such as `production` or `staging`. The server uses it for filtering, per-environment notifications, and the project's environment allow-list.

```csharp
client.Configuration.SetEnvironment("production");                         // default for every event
client.CreateLog("Deployment complete").SetEnvironment("staging").Submit(); // override one event
```

The first match in this order wins:

| # | Source | Example |
|---|---|---|
| 1 | Per event | `builder.SetEnvironment("staging")` |
| 2 | Code | `config.SetEnvironment("production")`, or `config.Environment` inside `AddExceptionless(c => ...)` |
| 3 | Configuration (later sources replace earlier ones) | `Exceptionless:Environment` in `appsettings.json` · `Exceptionless__Environment` env var · `[assembly: Exceptionless(..., Environment = "production")]` · `<exceptionless environment="production" />` · `Exceptionless:Environment` app setting |
| 4 | Host | `IHostEnvironment.EnvironmentName`, for the Generic Host, ASP.NET Core and Worker services |
| 5 | Process | the first of `ASPNETCORE_ENVIRONMENT`, `DOTNET_ENVIRONMENT` or `AZURE_FUNCTIONS_ENVIRONMENT` that is set |

The NLog target and the log4net appenders also accept an `Environment` setting, which overrides per event. For NLog it can be a layout such as `${environment:DOTNET_ENVIRONMENT}`.

- **Valid names:** names are trimmed, then must match `^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$`. Casing is kept and the server matches case-insensitively.
- **Invalid names:** a configured invalid or empty value, such as `Staging West`, `prod/eu` or `produção`, leaves events unspecified. It does not fall back to a lower source.
- **Not `data.@environment`:** the event's `environment` is separate from the machine diagnostics in `data.@environment`. Stacks and fixed versions are shared across environments.
- **Legacy ASP.NET, WinForms and WPF:** these apps have no host environment, so set the environment through configuration or code.

## Getting Started (Development)

All of our [.NET clients can be installed](https://www.nuget.org/profiles/exceptionless?showAllPackages=True) via the [NuGet package manager](https://docs.nuget.org/consume/Package-Manager-Dialog).
If you need help, please contact us via in-app support or
[open an issue](https://github.com/exceptionless/Exceptionless.Net/issues/new).
We’re always here to help if you have any questions!

**This section is for development purposes only! If you are trying to use the
Exceptionless .NET libraries, please get them from NuGet.**

### Visual Studio

Using Windows and Visual Studio is preferred so all platforms can be built and
editor design surfaces are available.

1. You will need to install:
   1. [Visual Studio 2022](https://visualstudio.microsoft.com/vs/community/)
   2. [.NET 10 SDK with Visual Studio tooling](https://dotnet.microsoft.com/download)
   3. [.NET Framework 4.7.2 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net472)
2. Open the `Exceptionless.Net.slnx` Visual Studio solution file.
3. Select `Exceptionless.SampleConsole` as the startup project.
4. Run the project by pressing `F5` to start the console.

### Visual Studio Code

You can also use [Visual Studio Code](https://code.visualstudio.com) and build
on macOS or Linux. You lose some of the rich design surfaces and the ability to
build windows specific packages.

1. You will need to install:
   1. [Visual Studio Code](https://code.visualstudio.com)
   2. [.NET 10 SDK](https://dotnet.microsoft.com/download)
2. Open the cloned Exceptionless.Net folder.
3. Run the `Exceptionless.SampleConsole` project by pressing `F5` to start the console.

## Thanks

Thanks to all the people who have contributed!

[![contributors](https://contributors-img.web.app/image?repo=exceptionless/exceptionless.net)](https://github.com/exceptionless/exceptionless.net/graphs/contributors)
