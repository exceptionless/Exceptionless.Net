using Exceptionless.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace Exceptionless.SampleMaui;

public static class MauiProgram {
    public static MauiApp CreateMauiApp() {
        var builder = MauiApp.CreateBuilder();
        var exceptionlessClient = ConfigureExceptionlessClient();

        builder
            .UseMauiApp<App>();

        builder.Services.AddSingleton(exceptionlessClient);
        builder.Services.AddSingleton<SampleEventService>();
        builder.Services.AddSingleton<SampleDogfoodRunner>();
        builder.Services.AddSingleton<MainPage>();

        return builder.Build();
    }

    private static ExceptionlessClient ConfigureExceptionlessClient() {
        string appDataDirectory = FileSystem.Current.AppDataDirectory;
        var client = ExceptionlessClient.Default;
        var config = client.Configuration;
        string? apiKey = Environment.GetEnvironmentVariable("EXCEPTIONLESS_API_KEY");
        if (!String.IsNullOrWhiteSpace(apiKey))
            config.ApiKey = apiKey;

        string? serverUrl = Environment.GetEnvironmentVariable("EXCEPTIONLESS_SERVER_URL");
        if (!String.IsNullOrWhiteSpace(serverUrl))
            config.ServerUrl = serverUrl;

        config.IncludePrivateInformation = false;
        config.DefaultTags.Add("maui");
        config.DefaultTags.Add("sample");
        config.DefaultData["Platform"] = DeviceInfo.Current.Platform.ToString();
        config.DefaultData["DeviceIdiom"] = DeviceInfo.Current.Idiom.ToString();
        config.SetVersion(AppInfo.Current.VersionString);
        config.UseFolderStorage(Path.Join(appDataDirectory, "exceptionless-queue"));
        config.UseFileLogger(Path.Join(appDataDirectory, "exceptionless-client.log"), LogLevel.Info);

        client.Startup();
        return client;
    }
}
