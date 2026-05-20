namespace WebView22Browser.Core;

public sealed class BrowserOptions
{
    public string HomeUrl { get; set; } = "https://www.bing.com";

    public string SearchUrlTemplate { get; set; } = "https://www.bing.com/search?q={0}";

    public string? UserDataRoot { get; set; }

    public string? FavoritesFilePath { get; set; }

    public string? ExtensionsRegistryFilePath { get; set; }

    public string? DownloadHistoryFilePath { get; set; }

    public string? BrowsingHistoryFilePath { get; set; }

    public string? UserScriptsFilePath { get; set; }

    public string? GmStorageDirectoryPath { get; set; }

    /// <summary>Maximum persisted download history entries (excludes in-progress).</summary>
    public int DownloadHistoryMaxEntries { get; set; } = 200;

    /// <summary>Maximum persisted browsing history entries.</summary>
    public int BrowsingHistoryMaxEntries { get; set; } = 2000;

    /// <summary>Minutes of inactivity before a background tab may sleep. 0 disables tab sleeping.</summary>
    public int TabSleepTimeoutMinutes { get; set; } = 5;

    /// <summary>How often the tab sleep scanner runs, in seconds.</summary>
    public int TabSleepCheckIntervalSeconds { get; set; } = 30;

    /// <summary>Maximum navigation history entries tracked per tab for sleep restore.</summary>
    public int TabHistoryMaxEntries { get; set; } = 50;

    /// <summary>Restore tabs and navigation stacks from tabs-session.json on startup.</summary>
    public bool RestoreLastSession { get; set; } = true;

    public string? TabSessionFilePath { get; set; }

    /// <summary>System memory load percent at or above which pressure is Elevated.</summary>
    public int PressureElevatedMemoryPercent { get; set; } = 70;

    /// <summary>System memory load percent at or above which pressure is High.</summary>
    public int PressureHighMemoryPercent { get; set; } = 85;

    /// <summary>Process CPU percent at or above which pressure may be High (combined with memory).</summary>
    public int PressureHighCpuPercent { get; set; } = 80;

    /// <summary>EMA window for pressure sampling, in seconds.</summary>
    public int PressureSampleWindowSeconds { get; set; } = 15;

    public string GetDefaultTabSessionPath() =>
        TabSessionFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "tabs-session.json");

    public string GetDefaultFavoritesPath() =>
        FavoritesFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "favorites.json");

    public string GetDefaultExtensionsRegistryPath() =>
        ExtensionsRegistryFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "extensions.json");

    public string GetDefaultDownloadHistoryPath() =>
        DownloadHistoryFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "download-history.json");

    public string GetDefaultBrowsingHistoryPath() =>
        BrowsingHistoryFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "browsing-history.json");

    public string GetDefaultUserScriptsPath() =>
        UserScriptsFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "userscripts.json");

    public string GetDefaultGmStorageDirectory() =>
        GmStorageDirectoryPath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "gm-storage");

    public string GetUserDataFolder() =>
        Path.Combine(
            UserDataRoot ?? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WebView22Browser",
            "UserData",
            "Profile");
}