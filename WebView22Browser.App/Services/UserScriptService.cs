using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class UserScriptService
{
    private static readonly IReadOnlyDictionary<Guid, string> EmptyNonces =
        new Dictionary<Guid, string>();

    private const string RequireGlobalShim = """
        ;(function(g){try{
        var jq=typeof jQuery!=='undefined'?jQuery:null;
        if(!jq&&typeof module!=='undefined'&&module.exports){
        var e=module.exports;if(e&&(e.fn||e.jquery))jq=e;}
        if(jq){g.jQuery=jq;g.$=jq;}
        if(typeof Swal!=='undefined'){g.Swal=Swal;}
        if(typeof hotkeys!=='undefined'){g.hotkeys=hotkeys;}
        }catch(err){}})(typeof globalThis!=='undefined'?globalThis:window);
        """;

    private const string JQueryDocumentCreatedSuffix = """
        ;try{if(typeof jQuery!=='undefined'){var g=typeof globalThis!=='undefined'?globalThis:window;g.jQuery=jQuery;g.$=jQuery;}}catch(e){}
        """;

    private readonly IUserScriptStore _store;
    private readonly UserScriptBootstrapBuilder _bootstrapBuilder;
    private readonly UserScriptBridge _bridge;
    private readonly IGmStorageStore _gmStore;
    private readonly ITabHostService _tabHostService;
    private readonly IUserScriptDependencyResolver _dependencyResolver;
    private readonly ConcurrentDictionary<CoreWebView2, List<string>> _registrationIds = new();

    public UserScriptService(
        IUserScriptStore store,
        UserScriptBootstrapBuilder bootstrapBuilder,
        UserScriptBridge bridge,
        IGmStorageStore gmStore,
        ITabHostService tabHostService,
        IUserScriptDependencyResolver dependencyResolver)
    {
        _store = store;
        _bootstrapBuilder = bootstrapBuilder;
        _bridge = bridge;
        _gmStore = gmStore;
        _tabHostService = tabHostService;
        _dependencyResolver = dependencyResolver;
    }

    public event Action? ScriptsChanged;

    public IUserScriptStore Store => _store;

    public async Task LoadAsync(CancellationToken cancellationToken = default) =>
        await _store.LoadAsync(cancellationToken);

    public async Task ApplyToWebViewAsync(
        CoreWebView2 core,
        CancellationToken cancellationToken = default,
        IReadOnlyDictionary<Guid, ResolvedScriptDependencies>? resolvedDependencies = null)
    {
        RemoveAllDocumentCreatedScripts(core);

        var enabled = _store.Items.Where(s => s.Enabled).ToList();
        var preloaded = new Dictionary<Guid, IReadOnlyDictionary<string, JsonElement>>();

        foreach (var script in enabled)
        {
            if (!UserScriptGrantHelper.HasStorageGrant(script.Grants))
                continue;

            preloaded[script.Id] = await _gmStore.LoadAsync(script.Id, cancellationToken);
        }

        resolvedDependencies ??= await _dependencyResolver.ResolveAllAsync(enabled, cancellationToken);

        if (enabled.Count == 0)
        {
            _bridge.RegisterNonces(core, EmptyNonces);
            return;
        }

        var registrationIds = new List<string>();

        foreach (var script in enabled)
        {
            if (!resolvedDependencies.TryGetValue(script.Id, out var deps))
                continue;

            var requireCount = Math.Min(script.RequireUrls.Length, deps.RequireScripts.Length);
            for (var i = 0; i < requireCount; i++)
            {
                var prepared = PrepareRequireScript(deps.RequireScripts[i], script.RequireUrls[i]);
                var requireId = await core.AddScriptToExecuteOnDocumentCreatedAsync(prepared);
                registrationIds.Add(requireId);
            }
        }

        var bootstrapDeps = StripRequireScriptsForBootstrap(resolvedDependencies);
        var artifact = _bootstrapBuilder.Build(enabled, preloaded, bootstrapDeps);
        if (artifact is null)
        {
            _bridge.RegisterNonces(core, EmptyNonces);
            _registrationIds[core] = registrationIds;
            return;
        }

        _bridge.RegisterNonces(core, artifact.NoncesByScriptId);
        var bootstrapId = await core.AddScriptToExecuteOnDocumentCreatedAsync(artifact.JavaScript);
        registrationIds.Add(bootstrapId);
        _registrationIds[core] = registrationIds;
    }

    public async Task<IReadOnlyDictionary<Guid, ResolvedScriptDependencies>> RefreshAllHostsAsync(
        CancellationToken cancellationToken = default)
    {
        ScriptsChanged?.Invoke();

        var enabled = _store.Items.Where(s => s.Enabled).ToList();
        var resolvedDependencies = await _dependencyResolver.ResolveAllAsync(enabled, cancellationToken);

        var applyTasks = _tabHostService.GetAllHosts()
            .Where(host => host.Tab?.IsWebViewReady == true && host.CoreWebView2 != null)
            .Select(host => ApplyToWebViewAsync(host.CoreWebView2!, cancellationToken, resolvedDependencies))
            .ToList();

        if (applyTasks.Count > 0)
            await Task.WhenAll(applyTasks);

        return resolvedDependencies;
    }

    public async Task ReinjectPageEnvironmentAsync(CoreWebView2 core, CancellationToken cancellationToken = default)
    {
        var enabled = _store.Items.Where(s => s.Enabled).ToList();
        if (enabled.Count == 0)
            return;

        var resolved = await _dependencyResolver.ResolveAllAsync(enabled, cancellationToken);

        foreach (var script in enabled)
        {
            if (!resolved.TryGetValue(script.Id, out var deps))
                continue;

            var requireCount = Math.Min(script.RequireUrls.Length, deps.RequireScripts.Length);
            for (var i = 0; i < requireCount; i++)
            {
                var prepared = PrepareRequireScript(deps.RequireScripts[i], script.RequireUrls[i]);
                var literal = JsonSerializer.Serialize(prepared);
                await core.ExecuteScriptAsync($"eval({literal})");
            }
        }

        await core.ExecuteScriptAsync("window.__wv2browserRerunMatched && window.__wv2browserRerunMatched();");
    }

    public Task DeleteScriptStorageAsync(Guid scriptId, CancellationToken cancellationToken = default) =>
        _gmStore.DeleteScriptAsync(scriptId, cancellationToken);

    public void UnregisterWebView(CoreWebView2 core)
    {
        RemoveAllDocumentCreatedScripts(core);
        _bridge.ClearNonces(core);
        _bridge.ClearMenuCommands(core);
    }

    private static string PrepareRequireScript(string content, string sourceUrl)
    {
        var prepared = content + RequireGlobalShim;
        if (sourceUrl.Contains("jquery", StringComparison.OrdinalIgnoreCase))
            prepared += JQueryDocumentCreatedSuffix;
        return prepared;
    }

    private void RemoveAllDocumentCreatedScripts(CoreWebView2 core)
    {
        if (!_registrationIds.TryRemove(core, out var ids))
            return;

        foreach (var id in ids)
        {
            try
            {
                core.RemoveScriptToExecuteOnDocumentCreated(id);
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed between refresh and remove.
            }
        }
    }

    private static Dictionary<Guid, ResolvedScriptDependencies> StripRequireScriptsForBootstrap(
        IReadOnlyDictionary<Guid, ResolvedScriptDependencies> resolved)
    {
        var map = new Dictionary<Guid, ResolvedScriptDependencies>();
        foreach (var (id, deps) in resolved)
        {
            var stripped = new ResolvedScriptDependencies
            {
                RequireScripts = [],
                ResourceTexts = deps.ResourceTexts
            };
            stripped.Warnings.AddRange(deps.Warnings);
            map[id] = stripped;
        }

        return map;
    }
}