namespace WebView22Browser.Core.Models;

/// <summary>Persisted user overrides in user-settings.json.</summary>
public sealed class BrowserSettingsDto
{
    public string? HomeUrl { get; set; }

    public string? SearchUrlTemplate { get; set; }

    public int? TabSleepTimeoutMinutes { get; set; }

    public int? TabSleepCheckIntervalSeconds { get; set; }

    public int? TabHistoryMaxEntries { get; set; }

    public bool? RestoreLastSession { get; set; }

    public int? PressureElevatedMemoryPercent { get; set; }

    public int? PressureHighMemoryPercent { get; set; }

    public int? PressureHighCpuPercent { get; set; }

    public int? PressureSampleWindowSeconds { get; set; }

    public int? BrowsingHistoryMaxEntries { get; set; }

    public int? DownloadHistoryMaxEntries { get; set; }
}
