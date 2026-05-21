using System.Net.Http;
using System.Windows;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Async;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;
namespace WebView22Browser.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        FireAndForget.Run(() => OnStartupAsync(e), HandleStartupFailure);
    }

    private async Task OnStartupAsync(StartupEventArgs e)
    {
        Services = await ConfigureServicesAsync();
        var mainViewModel = Services.GetRequiredService<MainViewModel>();
        Services.GetRequiredService<WpfGmTabService>()
            .SetOpenHandler((url, activate) => mainViewModel.OpenNewTab(url, activate));

        var favoritesViewModel = Services.GetRequiredService<FavoritesViewModel>();
        favoritesViewModel.NavigateToFavorite = item => mainViewModel.OpenFavoriteCommand.Execute(item);

        var historyViewModel = Services.GetRequiredService<HistoryViewModel>();
        historyViewModel.NavigateToUrl = mainViewModel.NavigateToUrl;

        var mainWindow = Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
        base.OnStartup(e);
    }

    private void HandleStartupFailure(Exception ex)
    {
        MessageBox.Show(
            $"无法启动浏览器：{ex.Message}",
            "启动失败",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        Shutdown(1);
    }

    private static async Task<IServiceProvider> ConfigureServicesAsync()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var browserSection = configuration.GetSection("Browser");
        var appConfig = new BrowserAppConfig
        {
            HomeUrl = browserSection["HomeUrl"],
            SearchUrlTemplate = browserSection["SearchUrlTemplate"],
            TabSleepTimeoutMinutes = ParseNullableInt(browserSection["TabSleepTimeoutMinutes"]),
            TabSleepCheckIntervalSeconds = ParseNullableInt(browserSection["TabSleepCheckIntervalSeconds"]),
            RestoreLastSession = ParseNullableBool(browserSection["RestoreLastSession"]),
            PressureElevatedMemoryPercent = ParseNullableInt(browserSection["PressureElevatedMemoryPercent"]),
            PressureHighMemoryPercent = ParseNullableInt(browserSection["PressureHighMemoryPercent"]),
            PressureHighCpuPercent = ParseNullableInt(browserSection["PressureHighCpuPercent"]),
            PressureSampleWindowSeconds = ParseNullableInt(browserSection["PressureSampleWindowSeconds"])
        };

        var userSettingsStore = new JsonUserSettingsStore();
        await userSettingsStore.LoadAsync().ConfigureAwait(false);
        var options = BrowserOptionsLoader.Load(appConfig, userSettingsStore.Current);

        var services = new ServiceCollection();
        services.AddSingleton(appConfig);
        services.AddSingleton(options);
        services.AddSingleton<IUserSettingsStore>(userSettingsStore);
        services.AddSingleton<NavigationService>();
        services.AddSingleton<WebView2EnvironmentService>();
        services.AddSingleton<IWebView2EnvironmentAccessor>(sp => sp.GetRequiredService<WebView2EnvironmentService>());
        services.AddSingleton<IFavoritesStore, JsonFavoritesStore>();
        services.AddSingleton<IExtensionSourceStore, JsonExtensionSourceStore>();
        services.AddSingleton<IUserScriptStore, JsonUserScriptStore>();
        services.AddSingleton(new GmStorageQuota());
        services.AddSingleton<IGmStorageStore>(sp =>
            new JsonGmStorageStore(
                sp.GetRequiredService<BrowserOptions>().GetDefaultGmStorageDirectory(),
                sp.GetRequiredService<GmStorageQuota>()));
        services.AddSingleton<IUserScriptDependencyCache, UserScriptDependencyCache>();
        services.AddSingleton<IUserScriptDependencyResolver, UserScriptDependencyResolver>();
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
        services.AddSingleton<GmMenuCommandRegistry>();
        services.AddSingleton<IBrowserStatusReporter, WpfBrowserStatusReporter>();
        services.AddSingleton<UserScriptBridge>();
        services.AddSingleton<UserScriptService>();
        services.AddSingleton<ExtensionManifestReader>();
        services.AddSingleton<UserScriptExtensionConflictService>();
        services.AddSingleton<UserScriptImportService>();
        services.AddSingleton<BrowserExtensionService>();
        services.AddSingleton<BrowsingDataClearService>();
        services.AddSingleton<PermissionMemoryStore>();
        services.AddSingleton<IDialogService, WpfDialogService>();
        services.AddSingleton<IDesktopService, WpfDesktopService>();
        services.AddSingleton<IDownloadHistoryStore, JsonDownloadHistoryStore>();
        services.AddSingleton<IBrowsingHistoryStore, JsonBrowsingHistoryStore>();
        services.AddSingleton<DownloadsViewModel>();
        services.AddSingleton<IBrowsingHistoryService, BrowsingHistoryService>();
        services.AddSingleton<HistoryViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<IDownloadService, WebView2DownloadService>();
        services.AddSingleton<ITabHostService, TabHostService>();
        services.AddSingleton<IUiThreadMarshaller, WpfUiThreadMarshaller>();
        services.AddSingleton<ISystemPressureMonitor, SystemPressureMonitor>();
        services.AddSingleton<ITabSessionStore, JsonTabSessionStore>();
        services.AddSingleton<TabSleepService>();
        services.AddSingleton<IRuntimeBrowserSettingsApplier>(sp => sp.GetRequiredService<TabSleepService>());
        services.AddSingleton<FavoritesViewModel>();
        services.AddSingleton<ExtensionsViewModel>();
        services.AddSingleton<UserScriptsViewModel>();
        services.AddSingleton<UserScriptCommandsViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton(sp => new Lazy<MainViewModel>(() => sp.GetRequiredService<MainViewModel>()));
        services.AddSingleton<MainWindow>();
        return services.BuildServiceProvider();
    }

    private static int? ParseNullableInt(string? value) =>
        int.TryParse(value, out var parsed) ? parsed : null;

    private static bool? ParseNullableBool(string? value) =>
        bool.TryParse(value, out var parsed) ? parsed : null;
}