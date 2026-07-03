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

    public string? SubmitWarningLog() {
        _exceptionlessClient.SubmitLog("Exceptionless.SampleMaui.MainPage", "MAUI sample warning log.", LogLevel.Warn);
        return _exceptionlessClient.GetLastReferenceId();
    }

    public string? TrackFeatureUsage() {
        _exceptionlessClient.SubmitFeatureUsage("MauiSample.TrackFeature");
        return _exceptionlessClient.GetLastReferenceId();
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
