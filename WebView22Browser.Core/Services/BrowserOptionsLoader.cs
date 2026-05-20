using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class BrowserOptionsLoader
{
    public static BrowserOptions Load(BrowserAppConfig? appConfig, BrowserSettingsDto? userSettings)
    {
        var options = new BrowserOptions();
        ApplyAppConfig(options, appConfig);
        if (userSettings != null)
            ApplyUserOverrides(options, userSettings);
        return options;
    }

    public static void ApplyAppConfig(BrowserOptions target, BrowserAppConfig? appConfig)
    {
        if (appConfig == null)
            return;

        if (!string.IsNullOrWhiteSpace(appConfig.HomeUrl))
            target.HomeUrl = appConfig.HomeUrl.Trim();

        if (!string.IsNullOrWhiteSpace(appConfig.SearchUrlTemplate))
            target.SearchUrlTemplate = appConfig.SearchUrlTemplate.Trim();

        if (appConfig.TabSleepTimeoutMinutes.HasValue)
            target.TabSleepTimeoutMinutes = appConfig.TabSleepTimeoutMinutes.Value;

        if (appConfig.TabSleepCheckIntervalSeconds.HasValue)
            target.TabSleepCheckIntervalSeconds = appConfig.TabSleepCheckIntervalSeconds.Value;

        if (appConfig.RestoreLastSession.HasValue)
            target.RestoreLastSession = appConfig.RestoreLastSession.Value;

        if (appConfig.PressureElevatedMemoryPercent.HasValue)
            target.PressureElevatedMemoryPercent = appConfig.PressureElevatedMemoryPercent.Value;

        if (appConfig.PressureHighMemoryPercent.HasValue)
            target.PressureHighMemoryPercent = appConfig.PressureHighMemoryPercent.Value;

        if (appConfig.PressureHighCpuPercent.HasValue)
            target.PressureHighCpuPercent = appConfig.PressureHighCpuPercent.Value;

        if (appConfig.PressureSampleWindowSeconds.HasValue)
            target.PressureSampleWindowSeconds = appConfig.PressureSampleWindowSeconds.Value;
    }

    public static void ApplyUserOverrides(BrowserOptions target, BrowserSettingsDto userSettings)
    {
        if (!string.IsNullOrWhiteSpace(userSettings.HomeUrl))
            target.HomeUrl = userSettings.HomeUrl.Trim();

        if (!string.IsNullOrWhiteSpace(userSettings.SearchUrlTemplate))
            target.SearchUrlTemplate = userSettings.SearchUrlTemplate.Trim();

        if (userSettings.TabSleepTimeoutMinutes.HasValue)
            target.TabSleepTimeoutMinutes = userSettings.TabSleepTimeoutMinutes.Value;

        if (userSettings.TabSleepCheckIntervalSeconds.HasValue)
            target.TabSleepCheckIntervalSeconds = userSettings.TabSleepCheckIntervalSeconds.Value;

        if (userSettings.TabHistoryMaxEntries.HasValue)
            target.TabHistoryMaxEntries = userSettings.TabHistoryMaxEntries.Value;

        if (userSettings.RestoreLastSession.HasValue)
            target.RestoreLastSession = userSettings.RestoreLastSession.Value;

        if (userSettings.PressureElevatedMemoryPercent.HasValue)
            target.PressureElevatedMemoryPercent = userSettings.PressureElevatedMemoryPercent.Value;

        if (userSettings.PressureHighMemoryPercent.HasValue)
            target.PressureHighMemoryPercent = userSettings.PressureHighMemoryPercent.Value;

        if (userSettings.PressureHighCpuPercent.HasValue)
            target.PressureHighCpuPercent = userSettings.PressureHighCpuPercent.Value;

        if (userSettings.PressureSampleWindowSeconds.HasValue)
            target.PressureSampleWindowSeconds = userSettings.PressureSampleWindowSeconds.Value;

        if (userSettings.BrowsingHistoryMaxEntries.HasValue)
            target.BrowsingHistoryMaxEntries = userSettings.BrowsingHistoryMaxEntries.Value;

        if (userSettings.DownloadHistoryMaxEntries.HasValue)
            target.DownloadHistoryMaxEntries = userSettings.DownloadHistoryMaxEntries.Value;
    }

    public static BrowserSettingsDto FromOptions(BrowserOptions options) => new()
    {
        HomeUrl = options.HomeUrl,
        SearchUrlTemplate = options.SearchUrlTemplate,
        TabSleepTimeoutMinutes = options.TabSleepTimeoutMinutes,
        TabSleepCheckIntervalSeconds = options.TabSleepCheckIntervalSeconds,
        TabHistoryMaxEntries = options.TabHistoryMaxEntries,
        RestoreLastSession = options.RestoreLastSession,
        PressureElevatedMemoryPercent = options.PressureElevatedMemoryPercent,
        PressureHighMemoryPercent = options.PressureHighMemoryPercent,
        PressureHighCpuPercent = options.PressureHighCpuPercent,
        PressureSampleWindowSeconds = options.PressureSampleWindowSeconds,
        BrowsingHistoryMaxEntries = options.BrowsingHistoryMaxEntries,
        DownloadHistoryMaxEntries = options.DownloadHistoryMaxEntries
    };

    public static BrowserSettingsDto DefaultsFromAppConfig(BrowserAppConfig? appConfig)
    {
        var options = Load(appConfig, null);
        return FromOptions(options);
    }
}
