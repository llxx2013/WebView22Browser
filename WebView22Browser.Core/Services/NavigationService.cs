namespace WebView22Browser.Core.Services;

public sealed class NavigationService
{
    private readonly BrowserOptions _options;

    public NavigationService(BrowserOptions options)
    {
        _options = options;
    }

    private const int MaxSearchQueryLength = 2048;

    public string ResolveUri(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return _options.HomeUrl;

        var trimmed = input.Trim();

        if (trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri) &&
                (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
            {
                return trimmed;
            }

            return BuildSearchUrl(trimmed);
        }

        if (trimmed.StartsWith("file:", StringComparison.OrdinalIgnoreCase) &&
            TryNormalizeFileUri(trimmed, out var fileUri))
        {
            return fileUri;
        }

        if (LooksLikeHost(trimmed) && TryBuildHttpsUri(trimmed, out var httpsUri))
            return httpsUri;

        return BuildSearchUrl(trimmed);
    }

    private string BuildSearchUrl(string query)
    {
        if (query.Length > MaxSearchQueryLength)
            query = query[..MaxSearchQueryLength];

        try
        {
            return string.Format(_options.SearchUrlTemplate, Uri.EscapeDataString(query));
        }
        catch (UriFormatException)
        {
            return _options.HomeUrl;
        }
    }

    private static bool LooksLikeHost(string trimmed)
    {
        if (trimmed.Contains(' '))
            return false;

        if (trimmed.StartsWith("localhost", StringComparison.OrdinalIgnoreCase))
            return true;

        if (trimmed.StartsWith("127.0.0.1", StringComparison.OrdinalIgnoreCase))
            return true;

        if (trimmed.Contains('.'))
            return true;

        return false;
    }

    private static bool TryNormalizeFileUri(string trimmed, out string uri)
    {
        uri = string.Empty;
        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var absoluteUri) ||
            !absoluteUri.IsFile)
        {
            return false;
        }

        uri = absoluteUri.AbsoluteUri;
        return true;
    }

    private static bool TryBuildHttpsUri(string trimmed, out string uri)
    {
        uri = string.Empty;
        if (!Uri.TryCreate("https://" + trimmed, UriKind.Absolute, out var httpsUri) ||
            string.IsNullOrEmpty(httpsUri.Host))
        {
            return false;
        }

        uri = httpsUri.ToString();
        return true;
    }
}