namespace WebView22Browser.App.Services;

public sealed class TabHostService : ITabHostService
{
    private readonly Dictionary<Guid, ITabWebViewHost> _hosts = new();

    public void Register(Guid tabId, ITabWebViewHost host) => _hosts[tabId] = host;

    public void Unregister(Guid tabId) => _hosts.Remove(tabId);

    public ITabWebViewHost? GetHost(Guid tabId) =>
        _hosts.TryGetValue(tabId, out var host) ? host : null;

    public IReadOnlyCollection<ITabWebViewHost> GetAllHosts() => _hosts.Values.ToList();
}