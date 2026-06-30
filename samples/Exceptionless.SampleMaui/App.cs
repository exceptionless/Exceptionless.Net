namespace Exceptionless.SampleMaui;

public sealed class App : Application {
    private readonly ExceptionlessClient _exceptionlessClient;
    private readonly MainPage _mainPage;

    public App(MainPage mainPage, ExceptionlessClient exceptionlessClient) {
        _exceptionlessClient = exceptionlessClient;
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        return new Window(_mainPage);
    }

    protected override void OnSleep() {
        _ = _exceptionlessClient.ProcessQueueAsync();
        base.OnSleep();
    }
}
