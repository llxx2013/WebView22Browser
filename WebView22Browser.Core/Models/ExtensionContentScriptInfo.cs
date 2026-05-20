namespace WebView22Browser.Core.Models;

public sealed class ExtensionContentScriptInfo
{
    public string ExtensionName { get; set; } = string.Empty;

    public string ExtensionId { get; set; } = string.Empty;

    public string[] MatchPatterns { get; set; } = [];
}