using Exceptionless.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace Exceptionless.SampleMaui;

public static class MauiProgram {
    private const string DefaultApiKey = "LhhP1C9gijpSKCslHHCvwdSIz298twx271nTest";
    private const string DefaultServerUrl = "https://ex.dev.localhost:7111";

    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        var exceptionlessClient = CreateExceptionlessClient();

        builder
            .UseMauiApp<App>();

        builder.Services.AddSingleton(exceptionlessClient);
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }

    private static ExceptionlessClient CreateExceptionlessClient() {
        string appDataDirectory = FileSystem.Current.AppDataDirectory;

        var client = new ExceptionlessClient(config => {
            config.ApiKey = Environment.GetEnvironmentVariable("EXCEPTIONLESS_API_KEY") ?? DefaultApiKey;
            config.ServerUrl = Environment.GetEnvironmentVariable("EXCEPTIONLESS_SERVER_URL") ?? DefaultServerUrl;
            config.IncludePrivateInformation = false;
            config.DefaultTags.Add("maui");
            config.DefaultTags.Add("sample");
            config.DefaultData["Platform"] = DeviceInfo.Current.Platform.ToString();
            config.DefaultData["DeviceIdiom"] = DeviceInfo.Current.Idiom.ToString();
            config.SetVersion(AppInfo.Current.VersionString);
            config.UseFolderStorage(Path.Combine(appDataDirectory, "exceptionless-queue"));
            config.UseFileLogger(Path.Combine(appDataDirectory, "exceptionless-client.log"), LogLevel.Info);
        });

        client.Startup();
        return client;
    }
}
