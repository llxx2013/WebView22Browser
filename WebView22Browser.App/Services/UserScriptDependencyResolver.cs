using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

public sealed class UserScriptDependencyResolver : IUserScriptDependencyResolver
{
    private readonly IUserScriptDependencyCache _cache;

    public UserScriptDependencyResolver(IUserScriptDependencyCache cache) => _cache = cache;

    public async Task<ResolvedScriptDependencies> ResolveAsync(
        UserScriptEntry entry,
        CancellationToken cancellationToken = default)
    {
        var requireScripts = new List<string>();
        var resourceTexts = new Dictionary<string, string>(StringComparer.Ordinal);
        var warnings = new List<string>();

        foreach (var url in entry.RequireUrls)
        {
            var fetch = await _cache.GetOrFetchAsync(url, cancellationToken);
            if (fetch.Success && fetch.Content != null)
                requireScripts.Add(fetch.Content);
            else
                warnings.Add($"无法加载 @require：{url}（{fetch.Error ?? "未知错误"}）");
        }

        foreach (var (name, url) in entry.Resources)
        {
            var fetch = await _cache.GetOrFetchAsync(url, cancellationToken);
            if (fetch.Success && fetch.Content != null)
                resourceTexts[name] = fetch.Content;
            else
                warnings.Add($"无法加载 @resource「{name}」：{url}（{fetch.Error ?? "未知错误"}）");
        }

        var resolved = new ResolvedScriptDependencies
        {
            RequireScripts = requireScripts.ToArray(),
            ResourceTexts = resourceTexts
        };
        resolved.Warnings.AddRange(warnings);
        return resolved;
    }

    public async Task<IReadOnlyDictionary<Guid, ResolvedScriptDependencies>> ResolveAllAsync(
        IEnumerable<UserScriptEntry> entries,
        CancellationToken cancellationToken = default)
    {
        var map = new Dictionary<Guid, ResolvedScriptDependencies>();
        foreach (var entry in entries.Where(e => e.Enabled))
        {
            var resolved = await ResolveAsync(entry, cancellationToken);
            map[entry.Id] = resolved;
        }

        return map;
    }
}