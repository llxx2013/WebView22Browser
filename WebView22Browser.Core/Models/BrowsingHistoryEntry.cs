namespace WebView22Browser.Core.Models;

public sealed class BrowsingHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Url { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public DateTime VisitedAtUtc { get; set; } = DateTime.UtcNow;
}
