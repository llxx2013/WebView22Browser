namespace WebView22Browser.Core.Models;

public sealed class ExtensionSourceEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FolderPath { get; set; } = string.Empty;

    public string? LastKnownExtensionId { get; set; }
}