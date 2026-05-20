namespace WebView22Browser.Core.Models;

public sealed class GmXhrRequest
{
    public required string RequestId { get; init; }

    public string Method { get; init; } = "GET";

    public required string Url { get; init; }

    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    public string? Data { get; init; }

    public int TimeoutMs { get; init; }

    public string ResponseType { get; init; } = "text";
}