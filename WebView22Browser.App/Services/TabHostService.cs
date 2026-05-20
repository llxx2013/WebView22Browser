using WebView22Browser.App.Controls;

namespace WebView22Browser.App.Services;

public sealed class TabHostService : ITabHostService
{
    private readonly Dictionary<Guid, TabWebViewHost> _hosts = new();

    public void Register(Guid tabId, TabWebViewHost host) => _hosts[tabId] = host;

    public void Unregister(Guid tabId) => _hosts.Remove(tabId);

    public TabWebViewHost? GetHost(Guid tabId) =>
        _hosts.TryGetValue(tabId, out var host) ? host : null;

    public IReadOnlyCollection<TabWebViewHost> GetAllHosts() => _hosts.Values.ToList();
}
