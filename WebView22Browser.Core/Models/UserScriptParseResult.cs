namespace WebView22Browser.Core.Models;

public sealed class UserScriptParseResult
{
    public string Name { get; set; } = string.Empty;

    public string[] MatchPatterns { get; set; } = [];

    public string[] ExcludePatterns { get; set; } = [];

    public UserScriptRunAt RunAt { get; set; } = UserScriptRunAt.DocumentStart;

    public bool RunInTopFrameOnly { get; set; }

    public string[] Grants { get; set; } = [];

    public string[] ConnectPatterns { get; set; } = [];

    public string[] RequireUrls { get; set; } = [];

    public Dictionary<string, string> Resources { get; set; } = new(StringComparer.Ordinal);

    public string Code { get; set; } = string.Empty;

    public List<string> Warnings { get; } = [];
}