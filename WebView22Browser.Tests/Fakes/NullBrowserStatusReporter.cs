using WebView22Browser.App.Services;

namespace WebView22Browser.Tests.Fakes;

internal sealed class NullBrowserStatusReporter : IBrowserStatusReporter
{
    public static readonly NullBrowserStatusReporter Instance = new();

    public void Report(string message)
    {
    }
}