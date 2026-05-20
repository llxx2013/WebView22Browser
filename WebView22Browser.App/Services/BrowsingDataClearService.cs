using Microsoft.Web.WebView2.Core;

namespace WebView22Browser.App.Services;

public sealed class BrowsingDataClearService
{
    private CoreWebView2Profile? _profile;

    public bool IsReady => _profile != null;

    public void SetProfile(CoreWebView2Profile profile) =>
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));

    public Task ClearAsync()
    {
        var profile = _profile
            ?? throw new InvalidOperationException("WebView2 尚未就绪。");

        var kinds = CoreWebView2BrowsingDataKinds.Cookies
            | CoreWebView2BrowsingDataKinds.DiskCache
            | CoreWebView2BrowsingDataKinds.BrowsingHistory
            | CoreWebView2BrowsingDataKinds.DownloadHistory;

        return profile.ClearBrowsingDataAsync(kinds);
    }
}