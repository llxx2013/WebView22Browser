using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Web.WebView2.Core;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class UserScriptService
{
    private static readonly IReadOnlyDictionary<Guid, string> EmptyNonces =
        new Dictionary<Guid, string>();

    private readonly IUserScriptStore _store;
    private readonly UserScriptBootstrapBuilder _bootstrapBuilder;
    private readonly UserScriptBridge _bridge;
    private readonly IGmStorageStore _gmStore;
    private readonly ITabHostService _tabHostService;
    private readonly ConcurrentDictionary<CoreWebView2, string> _registrationIds = new();

    public UserScriptService(
        IUserScriptStore store,
        UserScriptBootstrapBuilder bootstrapBuilder,
        UserScriptBridge bridge,
        IGmStorageStore gmStore,
        ITabHostService tabHostService)
    {
        _store = store;
        _bootstrapBuilder = bootstrapBuilder;
        _bridge = bridge;
        _gmStore = gmStore;
        _tabHostService = tabHostService;
    }

    public event Action? ScriptsChanged;

    public IUserScriptStore Store => _store;

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        await _store.LoadAsync(cancellationToken);

    public async Task ApplyToWebViewAsync(CoreWebView2 core, CancellationToken cancellationToken = default)
    {
        if (_registrationIds.TryRemove(core, out var existingId))
        {
            try
            {
                core.RemoveScriptToExecuteOnDocumentCreated(existingId);
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed between refresh and remove.
            }
        }

        var enabled = _store.Items.Where(s => s.Enabled).ToList();
        var preloaded = new Dictionary<Guid, IReadOnlyDictionary<string, JsonElement>>();

        foreach (var script in enabled)
        {
            if (!UserScriptGrantHelper.HasStorageGrant(script.Grants))
                continue;

            preloaded[script.Id] = await _gmStore.LoadAsync(script.Id, cancellationToken);
        }

        var artifact = _bootstrapBuilder.Build(enabled, preloaded);
        if (artifact is null)
        {
            _bridge.RegisterNonces(core, EmptyNonces);
            return;
        }

        _bridge.RegisterNonces(core, artifact.NoncesByScriptId);
        var scriptId = await core.AddScriptToExecuteOnDocumentCreatedAsync(artifact.JavaScript);
        _registrationIds[core] = scriptId;
    }

    public async Task RefreshAllHostsAsync(CancellationToken cancellationToken = default)
    {
        ScriptsChanged?.Invoke();

        var applyTasks = _tabHostService.GetAllHosts()
            .Where(host => host.Tab?.IsWebViewReady == true && host.CoreWebView2 != null)
            .Select(host => ApplyToWebViewAsync(host.CoreWebView2!, cancellationToken))
            .ToList();

        if (applyTasks.Count == 0)
            return;

        await Task.WhenAll(applyTasks);
    }

    public Task DeleteScriptStorageAsync(Guid scriptId, CancellationToken cancellationToken = default) =>
        _gmStore.DeleteScriptAsync(scriptId, cancellationToken);

    public void UnregisterWebView(CoreWebView2 core)
    {
        _registrationIds.TryRemove(core, out _);
        _bridge.ClearNonces(core);
        _bridge.ClearMenuCommands(core);
    }
}
