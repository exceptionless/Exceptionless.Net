using Exceptionless.Configuration;
using Exceptionless.Logging;

namespace Exceptionless.SampleMaui;

public sealed class SampleEventService {
    public const string SampleConfigSettingKey = "SampleMaui.ConfigValue";

    private readonly ExceptionlessClient _exceptionlessClient;

    public SampleEventService(ExceptionlessClient exceptionlessClient) {
        _exceptionlessClient = exceptionlessClient;
    }

    public string SubmitHandledException() {
        string referenceId = Guid.NewGuid().ToString("N");

        try {
            throw new InvalidOperationException("Exceptionless MAUI sample handled exception.");
        } catch (InvalidOperationException ex) {
            ex.ToExceptionless(_exceptionlessClient)
                .SetReferenceId(referenceId)
                .AddTags("handled")
                .SetProperty("Screen", nameof(MainPage))
                .Submit();
        }

        return referenceId;
    }

    public string SubmitWarningLog() {
        string referenceId = Guid.NewGuid().ToString("N");

        _exceptionlessClient.CreateLog("Exceptionless.SampleMaui.MainPage", "MAUI sample warning log.", LogLevel.Warn)
            .SetReferenceId(referenceId)
            .Submit();

        return referenceId;
    }

    public string TrackFeatureUsage() {
        string referenceId = Guid.NewGuid().ToString("N");

        _exceptionlessClient.CreateFeatureUsage("MauiSample.TrackFeature")
            .SetReferenceId(referenceId)
            .Submit();

        return referenceId;
    }

    public async Task RefreshProjectConfigurationAsync() {
        await SettingsManager.UpdateSettingsAsync(_exceptionlessClient.Configuration, 0);
    }

    public Task FlushQueueAsync() {
        return _exceptionlessClient.ProcessQueueAsync();
    }

    public string GetSampleConfigValue() {
        return _exceptionlessClient.Configuration.Settings.GetString(SampleConfigSettingKey, "not loaded");
    }
}
