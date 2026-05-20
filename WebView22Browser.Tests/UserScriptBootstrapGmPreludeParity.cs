namespace WebView22Browser.Tests;

/// <summary>
/// Ports GM grant prelude generation embedded in
/// <see cref="WebView22Browser.Core.Services.UserScriptBootstrapBuilder"/>.
/// </summary>
internal static class UserScriptBootstrapGmPreludeParity
{
    public static string BuildPrelude(IReadOnlyList<string> grants, IReadOnlySet<string> apiKeys)
    {
        var prelude = string.Empty;
        for (var i = 0; i < grants.Count; i++)
        {
            var grantName = grants[i];
            if (grantName.StartsWith("GM_", StringComparison.Ordinal)
                && apiKeys.Contains(grantName))
            {
                prelude += $"var {grantName} = GM.{grantName}; ";
            }
        }

        return prelude;
    }
}