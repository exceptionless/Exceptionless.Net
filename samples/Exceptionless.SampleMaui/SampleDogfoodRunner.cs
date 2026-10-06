namespace Exceptionless.SampleMaui;

public sealed class SampleDogfoodRunner {
    private readonly SampleEventService _sampleEvents;

    public SampleDogfoodRunner(SampleEventService sampleEvents) {
        _sampleEvents = sampleEvents;
    }

    public async Task RunIfRequestedAsync() {
        if (!String.Equals(Environment.GetEnvironmentVariable("EXCEPTIONLESS_SAMPLE_AUTORUN"), "true", StringComparison.OrdinalIgnoreCase))
            return;

        string? resultPath = Environment.GetEnvironmentVariable("EXCEPTIONLESS_SAMPLE_AUTORUN_RESULT_PATH");

        try {
            await _sampleEvents.RefreshProjectConfigurationAsync();
            string configValue = _sampleEvents.GetSampleConfigValue();
            string exceptionReferenceId = _sampleEvents.SubmitHandledException();
            string logReferenceId = _sampleEvents.SubmitWarningLog();
            string featureReferenceId = _sampleEvents.TrackFeatureUsage();
            await _sampleEvents.FlushQueueAsync();

            await WriteResultAsync(resultPath,
                "status=completed",
                $"config.{SampleEventService.SampleConfigSettingKey}={configValue}",
                $"handledExceptionReferenceId={exceptionReferenceId}",
                $"logReferenceId={logReferenceId}",
                $"featureReferenceId={featureReferenceId}");
        } catch (InvalidOperationException ex) {
            await WriteResultAsync(resultPath, "status=failed", $"error={ex.Message}");
        } catch (TaskCanceledException ex) {
            await WriteResultAsync(resultPath, "status=failed", $"error={ex.Message}");
        }
    }

    private static async Task WriteResultAsync(string? path, params string[] lines) {
        if (String.IsNullOrWhiteSpace(path))
            return;

        // Autorun should still exercise the client if a platform sandbox rejects the result path.
        try {
            await File.WriteAllLinesAsync(path, lines);
        } catch (IOException ex) {
            System.Diagnostics.Debug.WriteLine($"Unable to write Exceptionless MAUI sample dogfood result: {ex}");
        } catch (UnauthorizedAccessException ex) {
            System.Diagnostics.Debug.WriteLine($"Unable to write Exceptionless MAUI sample dogfood result: {ex}");
        }
    }
}
