namespace WebView22Browser.Core.Services;

public static class UserScriptUrlMatcher
{
    public static bool IsMatchAny(string url, IEnumerable<string> patterns)
    {
        foreach (var pattern in patterns)
        {
            if (IsMatch(url, pattern))
                return true;
        }

        return false;
    }

    public static bool IsMatch(string url, string pattern)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(pattern))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!TryParsePattern(pattern.Trim(), out var scheme, out var host, out var path))
            return false;

        if (IsRestrictedScheme(uri.Scheme))
        {
            if (!PatternAllowsScheme(pattern, uri.Scheme))
                return false;

            var slash = pattern.IndexOf("://", StringComparison.Ordinal);
            if (slash >= 0 && pattern[..slash] == "*")
                return false;
        }

        if (!SegmentMatches(uri.Scheme, scheme))
            return false;

        if (!SegmentMatches(uri.Host, host))
            return false;

        var uriPath = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        if (uri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase) &&
            !uriPath.StartsWith('/'))
        {
            uriPath = "/" + uriPath;
        }

        if (uri.Query.Length > 0)
            uriPath += uri.Query;

        return SegmentMatches(uriPath, path);
    }

    private static bool IsRestrictedScheme(string scheme) =>
        scheme.Equals("file", StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals("about", StringComparison.OrdinalIgnoreCase) ||
        scheme.Equals("edge", StringComparison.OrdinalIgnoreCase);

    private static bool PatternAllowsScheme(string pattern, string scheme)
    {
        var slash = pattern.IndexOf("://", StringComparison.Ordinal);
        if (slash < 0)
            return false;

        var patternScheme = pattern[..slash];
        return patternScheme.Equals(scheme, StringComparison.OrdinalIgnoreCase) ||
               patternScheme == "*";
    }

    private static bool TryParsePattern(string pattern, out string scheme, out string host, out string path)
    {
        scheme = string.Empty;
        host = string.Empty;
        path = string.Empty;

        var schemeEnd = pattern.IndexOf("://", StringComparison.Ordinal);
        if (schemeEnd < 0)
            return false;

        scheme = pattern[..schemeEnd];
        var remainder = pattern[(schemeEnd + 3)..];

        var pathStart = remainder.IndexOf('/');
        if (pathStart < 0)
        {
            host = remainder;
            path = "/*";
        }
        else
        {
            host = remainder[..pathStart];
            path = remainder[pathStart..];
        }

        return scheme.Length > 0 && host.Length > 0 && path.Length > 0;
    }

    private static bool SegmentMatches(string value, string pattern)
    {
        if (pattern == "*")
            return true;

        if (!pattern.Contains('*'))
            return string.Equals(value, pattern, StringComparison.OrdinalIgnoreCase);

        if (pattern.StartsWith('*') && pattern.EndsWith('*') && pattern.Length >= 2)
        {
            var inner = pattern[1..^1];
            return value.Contains(inner, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith('*'))
        {
            var suffix = pattern[1..];
            return value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.EndsWith('*'))
        {
            var prefix = pattern[..^1];
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }

        var parts = pattern.Split('*');
        if (parts.Length == 0)
            return value.Length == 0;

        var index = 0;
        for (var i = 0; i < parts.Length; i++)
        {
            var part = parts[i];
            if (part.Length == 0)
                continue;

            var found = value.IndexOf(part, index, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
                return false;

            if (i == 0 && !pattern.StartsWith('*') && found != 0)
                return false;

            index = found + part.Length;
        }

        if (!pattern.EndsWith('*') && index != value.Length)
            return false;

        return true;
    }
}