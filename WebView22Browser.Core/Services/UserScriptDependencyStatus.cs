using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class UserScriptDependencyStatus
{
    public static bool IsCacheReady(UserScriptEntry entry, ResolvedScriptDependencies resolved) =>
        entry.RequireUrls.Length == resolved.RequireScripts.Length
        && entry.Resources.Count == resolved.ResourceTexts.Count
        && resolved.Warnings.Count == 0;

    public static bool HasDependencies(UserScriptEntry entry) =>
        entry.RequireUrls.Length > 0 || entry.Resources.Count > 0;

    public static string FormatSummary(
        UserScriptEntry entry,
        ResolvedScriptDependencies? resolved,
        bool isEnabled)
    {
        var requireCount = entry.RequireUrls.Length;
        var resourceCount = entry.Resources.Count;
        if (requireCount == 0 && resourceCount == 0)
            return string.Empty;

        var parts = new List<string>();
        if (requireCount > 0)
            parts.Add($"Requires {requireCount}");
        if (resourceCount > 0)
            parts.Add($"Resources {resourceCount}");

        if (!isEnabled)
        {
            parts.Add("已禁用");
            return string.Join(" · ", parts);
        }

        parts.Add(resolved != null && IsCacheReady(entry, resolved) ? "缓存就绪" : "依赖待刷新");
        return string.Join(" · ", parts);
    }
}
