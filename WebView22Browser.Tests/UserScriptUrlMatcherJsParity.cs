namespace WebView22Browser.Tests;

/// <summary>
/// Ports the matchUrl / segmentMatches logic embedded in <see cref="WebView22Browser.Core.Services.UserScriptBootstrapBuilder"/>.
/// Must stay aligned with generated bootstrap JavaScript; cross-validated via <see cref="UserScriptUrlMatcherParityTests"/>.
/// </summary>
internal static class UserScriptUrlMatcherJsParity
{
    public static bool IsMatch(string url, string pattern)
    {
        if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(pattern))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (!TryParsePattern(pattern.Trim(), out var scheme, out var host, out var path))
            return false;

        var uriScheme = uri.Scheme;
        if (IsRestrictedScheme(uriScheme))
        {
            if (!PatternAllowsScheme(pattern, uriScheme))
                return false;

            var slash = pattern.IndexOf("://", StringComparison.Ordinal);
            if (slash >= 0 && pattern[..slash] == "*")
                return false;
        }

        if (!SegmentMatches(uriScheme, scheme))
            return false;

        // JS URL.hostname — empty for file:///C:/...
        var hostname = uri.Host;
        if (!SegmentMatches(hostname, host))
            return false;

        var uriPath = string.IsNullOrEmpty(uri.AbsolutePath) ? "/" : uri.AbsolutePath;
        if (uriScheme.Equals("file", StringComparison.OrdinalIgnoreCase) && !uriPath.StartsWith('/'))
            uriPath = "/" + uriPath;

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
        return patternScheme == "*" ||
               patternScheme.Equals(scheme, StringComparison.OrdinalIgnoreCase);
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
            return value.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase);

        if (pattern.EndsWith('*'))
            return value.StartsWith(pattern[..^1], StringComparison.OrdinalIgnoreCase);

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

            if (i == 0 && !pattern.StartsWith('*') && found != 0)
                return false;

            index = found + part.Length;
        }

        if (!pattern.EndsWith('*') && index != value.Length)
            return false;

        return true;
    }
}