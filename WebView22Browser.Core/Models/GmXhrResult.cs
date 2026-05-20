namespace WebView22Browser.Core.Models;

public sealed class GmXhrResult
{
    public int Status { get; init; }

    public string StatusText { get; init; } = string.Empty;

    public IReadOnlyDictionary<string, string> ResponseHeaders { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public string? ResponseText { get; init; }

    public string? ResponseBase64 { get; init; }

    public string FinalUrl { get; init; } = string.Empty;
}
