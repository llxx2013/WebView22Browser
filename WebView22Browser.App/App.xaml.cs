using System.Net.Http;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Services = ConfigureServices();
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        Services.GetRequiredService<WpfGmTabService>()
            .SetOpenHandler(url => mainViewModel.OpenNewTab(url));

        var favoritesViewModel = Services.GetRequiredService<FavoritesViewModel>();
        favoritesViewModel.NavigateToFavorite = item => mainViewModel.OpenFavoriteCommand.Execute(item);

        var historyViewModel = Services.GetRequiredService<HistoryViewModel>();
        historyViewModel.NavigateToUrl = mainViewModel.NavigateToUrl;

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    private static IServiceProvider ConfigureServices()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var browserSection = configuration.GetSection("Browser");
        var options = new BrowserOptions
        {
            HomeUrl = browserSection["HomeUrl"] ?? "https://www.bing.com",
            SearchUrlTemplate = browserSection["SearchUrlTemplate"] ?? "https://www.bing.com/search?q={0}",
            TabSleepTimeoutMinutes = ParseInt(browserSection["TabSleepTimeoutMinutes"], 5),
            TabSleepCheckIntervalSeconds = ParseInt(browserSection["TabSleepCheckIntervalSeconds"], 30),
            RestoreLastSession = ParseBool(browserSection["RestoreLastSession"], true),
            PressureElevatedMemoryPercent = ParseInt(browserSection["PressureElevatedMemoryPercent"], 70),
            PressureHighMemoryPercent = ParseInt(browserSection["PressureHighMemoryPercent"], 85),
            PressureHighCpuPercent = ParseInt(browserSection["PressureHighCpuPercent"], 80),
            PressureSampleWindowSeconds = ParseInt(browserSection["PressureSampleWindowSeconds"], 15)
        };

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddSingleton<NavigationService>();
        services.AddSingleton<WebView2EnvironmentService>();
        services.AddSingleton<IFavoritesStore, JsonFavoritesStore>();
        services.AddSingleton<IExtensionSourceStore, JsonExtensionSourceStore>();
        services.AddSingleton<IUserScriptStore, JsonUserScriptStore>();
        services.AddSingleton(new GmStorageQuota());
        services.AddSingleton<IGmStorageStore>(sp =>
            new JsonGmStorageStore(
                sp.GetRequiredService<BrowserOptions>().GetDefaultGmStorageDirectory(),
                sp.GetRequiredService<GmStorageQuota>()));
        services.AddSingleton<UserScriptBootstrapBuilder>();
        services.AddSingleton(_ => new HttpClient(new HttpClientHandler
        {
            UseCookies = false,
            AllowAutoRedirect = true
        })
        {
            Timeout = TimeSpan.FromMinutes(2)
        });
        services.AddSingleton<IGmXhrService, GmXhrService>();
        services.AddSingleton<GmXhrMessageHandler>();
        services.AddSingleton<WpfGmTabService>();
        services.AddSingleton<IGmTabService>(sp => sp.GetRequiredService<WpfGmTabService>());
        services.AddSingleton<IGmClipboardService, WpfGmClipboardService>();
        services.AddSingleton<UserScriptBridge>();
        services.AddSingleton<UserScriptService>();
        services.AddSingleton<ExtensionManifestReader>();
        services.AddSingleton<UserScriptExtensionConflictService>();
        services.AddSingleton<UserScriptImportService>();
        services.AddSingleton<BrowserExtensionService>();
        services.AddSingleton<PermissionMemoryStore>();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<IDesktopService, WpfDesktopService>();
        services.AddSingleton<IDownloadHistoryStore, JsonDownloadHistoryStore>();
        services.AddSingleton<IBrowsingHistoryStore, JsonBrowsingHistoryStore>();
        services.AddSingleton<DownloadsViewModel>();
        services.AddSingleton<IBrowsingHistoryService, BrowsingHistoryService>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<IDownloadService, WebView2DownloadService>();
        services.AddSingleton<ITabHostService, TabHostService>();
        services.AddSingleton<ISystemPressureMonitor, SystemPressureMonitor>();
        services.AddSingleton<ITabSessionStore, JsonTabSessionStore>();
        services.AddSingleton<TabSleepService>();
        services.AddSingleton<FavoritesViewModel>();
        services.AddSingleton<ExtensionsViewModel>();
        services.AddSingleton<UserScriptsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private static int ParseInt(string? value, int defaultValue) =>
        int.TryParse(value, out var parsed) ? parsed : defaultValue;

    private static bool ParseBool(string? value, bool defaultValue) =>
        bool.TryParse(value, out var parsed) ? parsed : defaultValue;
}
