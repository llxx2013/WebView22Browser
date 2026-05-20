namespace WebView22Browser.Core.Models;

/// <summary>Values from appsettings.json Browser section (factory defaults).</summary>
public sealed class BrowserAppConfig
{
    public string? HomeUrl { get; init; }

    public string? SearchUrlTemplate { get; init; }

    public int? TabSleepTimeoutMinutes { get; init; }

    public int? TabSleepCheckIntervalSeconds { get; init; }

    public bool? RestoreLastSession { get; init; }

    public int? PressureElevatedMemoryPercent { get; init; }

    public int? PressureHighMemoryPercent { get; init; }

    public int? PressureHighCpuPercent { get; init; }

    public int? PressureSampleWindowSeconds { get; init; }
}