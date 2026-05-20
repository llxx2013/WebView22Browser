namespace WebView22Browser.Core.Models;

public sealed class GmXhrResponseEnvelope
{
    public required string RequestId { get; init; }

    public required string ScriptId { get; init; }

    public required string Kind { get; init; }

    public object? Response { get; init; }

    public string? Error { get; init; }
}