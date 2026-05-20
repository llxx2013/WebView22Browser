using WebView22Browser.App.Controls;

namespace WebView22Browser.App.Services;

public interface ITabHostService
{
    void Register(Guid tabId, TabWebViewHost host);

    void Unregister(Guid tabId);

    TabWebViewHost? GetHost(Guid tabId);

    IReadOnlyCollection<TabWebViewHost> GetAllHosts();
}
