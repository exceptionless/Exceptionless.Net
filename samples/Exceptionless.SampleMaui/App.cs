namespace Exceptionless.SampleMaui;

public sealed class App : Application {
    private readonly ExceptionlessClient _exceptionlessClient;
    private readonly SampleDogfoodRunner _dogfoodRunner;
    private readonly MainPage _mainPage;

    public App(MainPage mainPage, ExceptionlessClient exceptionlessClient, SampleDogfoodRunner dogfoodRunner) {
        _exceptionlessClient = exceptionlessClient;
        _dogfoodRunner = dogfoodRunner;
        _mainPage = mainPage;
    }

    protected override Window CreateWindow(IActivationState? activationState) {
        var window = new Window(_mainPage);
        _ = _dogfoodRunner.RunIfRequestedAsync();

        return window;
    }

    protected override void OnSleep() {
        _ = _exceptionlessClient.ProcessQueueAsync();
        base.OnSleep();
    }
}
