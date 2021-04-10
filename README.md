# Exceptionless .NET Clients

[![Build Windows](https://github.com/exceptionless/Exceptionless.Net/workflows/Build%20Windows/badge.svg?branch=master)](https://github.com/Exceptionless/Exceptionless.Net/actions)
[![Build OSX](https://github.com/exceptionless/Exceptionless.Net/workflows/Build%20OSX/badge.svg)](https://github.com/Exceptionless/Exceptionless.Net/actions)
[![Build Linux](https://github.com/exceptionless/Exceptionless.Net/workflows/Build%20Linux/badge.svg)](https://github.com/Exceptionless/Exceptionless.Net/actions)
[![NuGet Version](http://img.shields.io/nuget/v/Exceptionless.svg?style=flat)](https://www.nuget.org/packages/Exceptionless/)
[![Discord](https://img.shields.io/discord/715744504891703319)](https://discord.gg/6HxgFCx)
[![Donate](https://img.shields.io/badge/donorbox-donate-blue.svg)](https://donorbox.org/exceptionless?recurring=true)

The definition of the word exceptionless is: to be without exception. [Exceptionless](https://exceptionless.io) provides real-time .NET error reporting for your ASP.NET, Web API, WebForms, WPF, Console, and MVC apps. It organizes the gathered information into simple actionable data that will help your app become exceptionless!

## Using Exceptionless

Refer to the Exceptionless documentation here: [Exceptionless Docs](http://docs.exceptionless.io).

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
   1. [Visual Studio 2019](https://visualstudio.microsoft.com/vs/community/)
   2. [.NET Core 5.x SDK with VS Tooling](https://dotnet.microsoft.com/download)
   3. [.NET Framework 4.6.2 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net462)
2. Open the `Exceptionless.Net.sln` Visual Studio solution file.
3. Select `Exceptionless.SampleConsole` as the startup project.
4. Run the project by pressing `F5` to start the console.

### Visual Studio Code

You can also use [Visual Studio Code](https://code.visualstudio.com) and build
on macOS or Linux. You lose some of the rich design surfaces and the ability to
build windows specific packages.

1. You will need to install:
   1. [Visual Studio Code](https://code.visualstudio.com)
   2. [.NET Core 5.x SDK with VS Tooling](https://dotnet.microsoft.com/download)
2. Open the cloned Exceptionless.Net folder.
3. Run the `Exceptionless.SampleConsole` project by pressing `F5` to start the console.

## Thanks

Thanks to all the people who have contributed!

[![contributors](https://contributors-img.web.app/image?repo=exceptionless/exceptionless.net)](https://github.com/exceptionless/exceptionless.net/graphs/contributors)
