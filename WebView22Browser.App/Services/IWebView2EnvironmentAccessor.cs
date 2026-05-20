namespace WebView22Browser.App.Services;

public interface IWebView2EnvironmentAccessor
{
    Task<string> GetBrowserVersionStringAsync(CancellationToken cancellationToken = default);
}