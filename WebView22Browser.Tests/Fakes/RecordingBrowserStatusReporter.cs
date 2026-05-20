using WebView22Browser.App.Services;

namespace WebView22Browser.Tests.Fakes;

internal sealed class RecordingBrowserStatusReporter : IBrowserStatusReporter
{
    public string? LastMessage { get; private set; }

    public void Report(string message) => LastMessage = message;
}