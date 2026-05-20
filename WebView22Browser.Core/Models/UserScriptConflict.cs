namespace WebView22Browser.Core.Models;

public sealed class UserScriptConflict
{
    public string ExtensionName { get; set; } = string.Empty;

    public string ExtensionId { get; set; } = string.Empty;

    public string UserPattern { get; set; } = string.Empty;

    public string ExtensionPattern { get; set; } = string.Empty;

    public string SampleUrl { get; set; } = string.Empty;
}
