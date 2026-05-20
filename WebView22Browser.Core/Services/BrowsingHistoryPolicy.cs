namespace WebView22Browser.Core.Services;

public static class BrowsingHistoryPolicy
{
    public static bool ShouldRecord(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not ("http" or "https" or "file"))
            return false;

        if (uri.Scheme == Uri.UriSchemeFile)
            return true;

        var host = uri.Host;
        if (string.IsNullOrEmpty(host))
            return false;

        // Defensive: block http(s)://blank/ style URLs. about:blank is handled below (host is empty).
        if (host.Equals("blank", StringComparison.OrdinalIgnoreCase))
            return false;

        if (url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return false;

        if (url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }
}