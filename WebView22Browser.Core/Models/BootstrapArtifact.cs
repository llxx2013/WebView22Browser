namespace WebView22Browser.Core.Models;

public sealed class BootstrapArtifact
{
    public required string JavaScript { get; init; }

    public required IReadOnlyDictionary<Guid, string> NoncesByScriptId { get; init; }
}
