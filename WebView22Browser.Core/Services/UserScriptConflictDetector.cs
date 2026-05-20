using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class UserScriptConflictDetector
{
    private static readonly string[] FixedProbeUrls =
    [
        "https://www.example.com/",
        "https://example.com/path",
        "https://www.bing.com/",
        "http://localhost/",
        "https://localhost:8080/test"
    ];

    public static IReadOnlyList<UserScriptConflict> FindConflicts(
        IEnumerable<string> userPatterns,
        IEnumerable<ExtensionContentScriptInfo> extensions)
    {
        var userList = userPatterns.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (userList.Count == 0)
            return [];

        var probes = BuildProbeUrls(userList, extensions);
        var conflicts = new List<UserScriptConflict>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var extension in extensions)
        {
            foreach (var userPattern in userList)
            {
                foreach (var extPattern in extension.MatchPatterns)
                {
                    foreach (var url in probes)
                    {
                        if (!UserScriptUrlMatcher.IsMatch(url, userPattern) ||
                            !UserScriptUrlMatcher.IsMatch(url, extPattern))
                            continue;

                        var key = $"{extension.ExtensionId}|{userPattern}|{extPattern}";
                        if (!seen.Add(key))
                            continue;

                        conflicts.Add(new UserScriptConflict
                        {
                            ExtensionName = extension.ExtensionName,
                            ExtensionId = extension.ExtensionId,
                            UserPattern = userPattern,
                            ExtensionPattern = extPattern,
                            SampleUrl = url
                        });
                        break;
                    }
                }
            }
        }

        return conflicts;
    }

    private static List<string> BuildProbeUrls(
        IReadOnlyList<string> userPatterns,
        IEnumerable<ExtensionContentScriptInfo> extensions)
    {
        var probes = new HashSet<string>(FixedProbeUrls, StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in userPatterns.Concat(extensions.SelectMany(e => e.MatchPatterns)))
        {
            foreach (var host in ExtractHosts(pattern))
            {
                if (host == "*")
                {
                    probes.Add("https://www.example.com/");
                    continue;
                }

                var normalized = host.StartsWith("*.") ? host[2..] : host;
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    probes.Add($"https://{normalized}/");
                    probes.Add($"https://www.{normalized}/");
                }
            }
        }

        return probes.ToList();
    }

    private static IEnumerable<string> ExtractHosts(string pattern)
    {
        var schemeEnd = pattern.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
            yield break;

        var remainder = pattern[(schemeEnd + 3)..];
        var pathStart = remainder.IndexOf('/');
        var host = pathStart < 0 ? remainder : remainder[..pathStart];
        if (!string.IsNullOrWhiteSpace(host))
            yield return host;
    }
}
