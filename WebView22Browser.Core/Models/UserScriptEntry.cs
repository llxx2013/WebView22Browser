namespace WebView22Browser.Core.Models;

public sealed class UserScriptEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string[] MatchPatterns { get; set; } = [];

    public string[] ExcludePatterns { get; set; } = [];

    public string Code { get; set; } = string.Empty;

    public UserScriptRunAt RunAt { get; set; } = UserScriptRunAt.DocumentStart;

    public bool RunInTopFrameOnly { get; set; }

    public string[] Grants { get; set; } = [];

    public string[] ConnectPatterns { get; set; } = [];

    public string[] RequireUrls { get; set; } = [];

    public Dictionary<string, string> Resources { get; set; } = new(StringComparer.Ordinal);

    public string? SourceFilePath { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public void CopyFrom(UserScriptEntry source)
    {
        Name = source.Name;
        Enabled = source.Enabled;
        MatchPatterns = source.MatchPatterns.ToArray();
        ExcludePatterns = source.ExcludePatterns.ToArray();
        Code = source.Code;
        RunAt = source.RunAt;
        RunInTopFrameOnly = source.RunInTopFrameOnly;
        Grants = source.Grants.ToArray();
        ConnectPatterns = source.ConnectPatterns.ToArray();
        RequireUrls = source.RequireUrls.ToArray();
        Resources = new Dictionary<string, string>(source.Resources, StringComparer.Ordinal);
        SourceFilePath = source.SourceFilePath;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}