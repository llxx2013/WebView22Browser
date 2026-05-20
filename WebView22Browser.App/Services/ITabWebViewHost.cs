using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

/// <summary>
/// Tab WebView host capabilities without WPF control types (testable on non-Windows hosts).
/// </summary>
public interface ITabWebViewHost
{
    BrowserTabViewModel? Tab { get; }

    CoreWebView2? CoreWebView2 { get; }

    void Shutdown();

    Task ReduceMemoryAsync();

    Task SuspendLightAsync();

    Task SuspendAsync();

    Task WakeAsync();

    TabNavigationSnapshot? TryGetHistorySnapshot();
}