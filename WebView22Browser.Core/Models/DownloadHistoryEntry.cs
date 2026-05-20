namespace WebView22Browser.Core.Models;

public sealed class DownloadHistoryEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string FileName { get; set; } = string.Empty;

    public string SourceUrl { get; set; } = string.Empty;

    public string FullPath { get; set; } = string.Empty;

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAtUtc { get; set; }

    public DownloadState State { get; set; }

    public long BytesReceived { get; set; }

    public long TotalBytes { get; set; }
}