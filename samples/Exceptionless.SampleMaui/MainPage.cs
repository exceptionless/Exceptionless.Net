using Microsoft.Maui.Controls.Shapes;

namespace Exceptionless.SampleMaui;

public sealed class MainPage : ContentPage {
    private readonly ExceptionlessClient _exceptionlessClient;
    private readonly SampleEventService _sampleEvents;
    private readonly Label _statusLabel;
    private readonly Label _lastReferenceIdLabel;
    private readonly Label _configLabel;
    private readonly ActivityIndicator _activityIndicator;

    public MainPage(ExceptionlessClient exceptionlessClient, SampleEventService sampleEvents) {
        _exceptionlessClient = exceptionlessClient;
        _sampleEvents = sampleEvents;

        Title = "Exceptionless";
        BackgroundColor = Color.FromArgb("#F6F8FA");

        _statusLabel = new Label {
            Text = "Ready",
            FontSize = 14,
            TextColor = Color.FromArgb("#314256"),
            LineBreakMode = LineBreakMode.WordWrap
        };

        _lastReferenceIdLabel = new Label {
            Text = "Last reference id: none",
            FontSize = 13,
            TextColor = Color.FromArgb("#576575"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _configLabel = new Label {
            Text = $"Config {SampleEventService.SampleConfigSettingKey}: not loaded",
            FontSize = 13,
            TextColor = Color.FromArgb("#576575"),
            LineBreakMode = LineBreakMode.TailTruncation
        };

        _activityIndicator = new ActivityIndicator {
            IsVisible = false,
            Color = Color.FromArgb("#276749")
        };

        Content = BuildContent();
    }

    private View BuildContent() {
        var refreshConfigButton = CreateActionButton("Refresh Config", OnRefreshConfigClicked);
        var sendExceptionButton = CreateActionButton("Send Handled Exception", OnSendExceptionClicked);
        var sendLogButton = CreateActionButton("Send Warning Log", OnSendLogClicked);
        var trackFeatureButton = CreateActionButton("Track Feature", OnTrackFeatureClicked);
        var flushButton = CreateActionButton("Flush Queue", OnFlushClicked);

        return new ScrollView {
            Content = new VerticalStackLayout {
                Padding = new Thickness(24, 28),
                Spacing = 18,
                Children = {
                    new Label {
                        Text = "Exceptionless MAUI Sample",
                        FontSize = 26,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = Color.FromArgb("#1D2733")
                    },
                    new Label {
                        Text = "Submit sample events through the core Exceptionless client.",
                        FontSize = 15,
                        TextColor = Color.FromArgb("#576575"),
                        LineBreakMode = LineBreakMode.WordWrap
                    },
                    new Border {
                        Stroke = Color.FromArgb("#D8DEE6"),
                        StrokeThickness = 1,
                        BackgroundColor = Colors.White,
                        StrokeShape = new RoundRectangle { CornerRadius = 8 },
                        Padding = new Thickness(18),
                        Content = new VerticalStackLayout {
                            Spacing = 12,
                            Children = {
                                new Label {
                                    Text = "Client",
                                    FontSize = 18,
                                    FontAttributes = FontAttributes.Bold,
                                    TextColor = Color.FromArgb("#1D2733")
                                },
                                new Label {
                                    Text = $"Server: {_exceptionlessClient.Configuration.ServerUrl}",
                                    FontSize = 13,
                                    TextColor = Color.FromArgb("#576575"),
                                    LineBreakMode = LineBreakMode.TailTruncation
                                },
                                new Label {
                                    Text = $"Private information: {_exceptionlessClient.Configuration.IncludePrivateInformation}",
                                    FontSize = 13,
                                    TextColor = Color.FromArgb("#576575")
                                },
                                _statusLabel,
                                _lastReferenceIdLabel,
                                _configLabel,
                                _activityIndicator
                            }
                        }
                    },
                    new VerticalStackLayout {
                        Spacing = 12,
                        Children = {
                            refreshConfigButton,
                            sendExceptionButton,
                            sendLogButton,
                            trackFeatureButton,
                            flushButton
                        }
                    }
                }
            }
        };
    }

    private static Button CreateActionButton(string text, EventHandler clicked) {
        var button = new Button {
            Text = text,
            BackgroundColor = Color.FromArgb("#285A84"),
            TextColor = Colors.White,
            CornerRadius = 8,
            FontAttributes = FontAttributes.Bold,
            MinimumHeightRequest = 48
        };

        button.Clicked += clicked;
        return button;
    }

    private async void OnRefreshConfigClicked(object? sender, EventArgs e) {
        await RunClientActionAsync("Configuration refreshed.", async () => {
            await _sampleEvents.RefreshProjectConfigurationAsync();
            SetConfigValue(_sampleEvents.GetSampleConfigValue());
        });
    }

    private async void OnSendExceptionClicked(object? sender, EventArgs e) {
        await RunClientActionAsync("Handled exception queued.", () => {
            string referenceId = _sampleEvents.SubmitHandledException();
            SetLastReferenceId(referenceId);
            return Task.CompletedTask;
        });
    }

    private async void OnSendLogClicked(object? sender, EventArgs e) {
        await RunClientActionAsync("Warning log queued.", () => {
            SetLastReferenceId(_sampleEvents.SubmitWarningLog());
            return Task.CompletedTask;
        });
    }

    private async void OnTrackFeatureClicked(object? sender, EventArgs e) {
        await RunClientActionAsync("Feature usage queued.", () => {
            SetLastReferenceId(_sampleEvents.TrackFeatureUsage());
            return Task.CompletedTask;
        });
    }

    private async void OnFlushClicked(object? sender, EventArgs e) {
        await RunClientActionAsync("Queue processed.", () => _sampleEvents.FlushQueueAsync());
    }

    private async Task RunClientActionAsync(string successMessage, Func<Task> action) {
        try {
            _activityIndicator.IsVisible = true;
            _activityIndicator.IsRunning = true;
            _statusLabel.Text = "Working...";

            await action();

            _statusLabel.Text = successMessage;
        } catch (InvalidOperationException ex) {
            _statusLabel.Text = $"Error: {ex.Message}";
        } catch (TaskCanceledException ex) {
            _statusLabel.Text = $"Error: {ex.Message}";
        } finally {
            _activityIndicator.IsRunning = false;
            _activityIndicator.IsVisible = false;
        }
    }

    private void SetLastReferenceId(string? referenceId) {
        _lastReferenceIdLabel.Text = String.IsNullOrEmpty(referenceId)
            ? "Last reference id: none"
            : $"Last reference id: {referenceId}";
    }

    private void SetConfigValue(string value) {
        _configLabel.Text = $"Config {SampleEventService.SampleConfigSettingKey}: {value}";
    }
}
