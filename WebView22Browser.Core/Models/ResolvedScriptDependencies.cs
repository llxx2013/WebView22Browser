namespace WebView22Browser.Core.Models;

public sealed class ResolvedScriptDependencies
{
    public string[] RequireScripts { get; init; } = [];

    public Dictionary<string, string> ResourceTexts { get; init; } = new(StringComparer.Ordinal);

    public List<string> Warnings { get; } = [];
}
