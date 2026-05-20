namespace WebView22Browser.App.Services;

public interface ITabHostService
{
    void Register(Guid tabId, ITabWebViewHost host);

    void Unregister(Guid tabId);

    ITabWebViewHost? GetHost(Guid tabId);

    IReadOnlyCollection<ITabWebViewHost> GetAllHosts();
}