using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.ViewModels;

namespace WebView22Browser.App.Services;

public interface IDownloadService
{
    void HandleDownloadStarting(CoreWebView2DownloadStartingEventArgs e, BrowserTabViewModel tab);

    void DetachTab(BrowserTabViewModel tab);
}