namespace WebView22Browser.Core.Services;

public static class BrowsingHistoryTitleFormatter
{
    public static string Format(string url, string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        return GetDisplayTitleFromUrl(url);
    }

    public static string GetDisplayTitleFromUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && !string.IsNullOrEmpty(uri.Host))
            return uri.Host;

        return url;
    }
}