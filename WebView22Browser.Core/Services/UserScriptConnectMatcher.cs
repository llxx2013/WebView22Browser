namespace WebView22Browser.Core.Services;

public static class UserScriptConnectMatcher
{
    public static bool IsAllowed(string targetUrl, string[] connectPatterns, string[] matchPatterns)
    {
        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var uri))
            return false;

        var scheme = uri.Scheme.ToLowerInvariant();
        if (scheme is not ("http" or "https"))
            return false;

        var host = uri.IdnHost;
        if (string.IsNullOrEmpty(host))
            return false;

        var patterns = connectPatterns.Length == 0 ? new[] { "self" } : connectPatterns;

        foreach (var raw in patterns)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var pattern = raw.Trim();
            if (pattern == "*")
                return true;

            if (string.Equals(pattern, "self", StringComparison.OrdinalIgnoreCase))
            {
                if (IsSelfAllowed(host, matchPatterns))
                    return true;

                continue;
            }

            if (HostMatchesConnectPattern(host, pattern))
                return true;
        }

        return false;
    }

    private static bool IsSelfAllowed(string targetHost, string[] matchPatterns)
    {
        foreach (var matchPattern in matchPatterns)
        {
            if (string.IsNullOrWhiteSpace(matchPattern))
                continue;

            var host = ExtractHostFromMatchPattern(matchPattern);
            if (string.IsNullOrEmpty(host))
                continue;

            // `self` is meant to scope outbound requests to hosts the script actually runs
            // on. A bare `*` host (e.g. `@match *://*/*`) would otherwise expand `self` into
            // a wildcard that allows every host, which violates Greasemonkey semantics and
            // defeats the purpose of @connect. Require a concrete host segment instead.
            if (host == "*")
                continue;

            if (HostMatchesPatternSegment(targetHost, host))
                return true;
        }

        return false;
    }

    internal static string? ExtractHostFromMatchPattern(string pattern)
    {
        var schemeEnd = pattern.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
            return null;

        var remainder = pattern[(schemeEnd + 3)..];
        var pathStart = remainder.IndexOf('/');
        return pathStart < 0 ? remainder : remainder[..pathStart];
    }

    private static bool HostMatchesConnectPattern(string host, string pattern)
    {
        if (pattern == "*")
            return true;

        if (pattern.StartsWith("*.", StringComparison.Ordinal))
        {
            var baseDomain = pattern[2..];
            if (string.IsNullOrEmpty(baseDomain))
                return false;

            return host.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.Contains('*', StringComparison.Ordinal))
            return HostMatchesPatternSegment(host, pattern);

        if (host.Equals(pattern, StringComparison.OrdinalIgnoreCase))
            return true;

        return host.EndsWith("." + pattern, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HostMatchesPatternSegment(string value, string pattern)
    {
        if (pattern == "*")
            return true;

        if (!pattern.Contains('*', StringComparison.Ordinal))
            return value.Equals(pattern, StringComparison.OrdinalIgnoreCase);

        if (pattern.StartsWith("*.") && pattern.IndexOf('*', 2) < 0)
        {
            var baseDomain = pattern[2..];
            return value.EndsWith("." + baseDomain, StringComparison.OrdinalIgnoreCase);
        }

        var parts = pattern.Split('*');
        var index = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
                continue;

            var found = value.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
                return false;

            if (i == 0 && pattern[0] != '*' && found != 0)
                return false;

            index = found + part.Length;
        }

        if (pattern[^1] != '*' && index != value.Length)
            return false;

        return true;
    }
}