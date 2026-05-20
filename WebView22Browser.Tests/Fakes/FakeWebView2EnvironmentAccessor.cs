using WebView22Browser.App.Services;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeWebView2EnvironmentAccessor : IWebView2EnvironmentAccessor
{
    public string BrowserVersion { get; init; } = "128.0.2739.42-test";

    public Task<string> GetBrowserVersionStringAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(BrowserVersion);
}