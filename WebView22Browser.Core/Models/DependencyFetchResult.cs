namespace WebView22Browser.Core.Models;

public sealed class DependencyFetchResult
{
    public bool Success { get; init; }

    public string? Content { get; init; }

    public string? Error { get; init; }
}