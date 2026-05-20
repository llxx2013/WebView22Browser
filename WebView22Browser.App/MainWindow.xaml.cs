using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Animations;
using WebView22Browser.App.Controls;
using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;

namespace WebView22Browser.App;

public partial class MainWindow : Window
{
    private const double FavoritesPanelWidth = 220;
    private const double UserScriptsPanelWidth = 300;
    private const double ExtensionsPanelWidth = 280;
    private const double DownloadsPanelHeight = 220;

    private readonly MainViewModel _viewModel;
    private readonly BrowserOptions _browserOptions;
    private readonly WebView2EnvironmentService _environmentService;
    private readonly IDialogService _dialogService;
    private readonly ITabHostService _tabHostService;
    private readonly PermissionMemoryStore _permissionStore;
    private readonly BrowserExtensionService _extensionService;
    private readonly BrowsingDataClearService _browsingDataClearService;
    private readonly UserScriptService _userScriptService;
    private readonly UserScriptBridge _userScriptBridge;
    private readonly TabSleepService _tabSleepService;
    private readonly IDownloadService _downloadService;
    private readonly IBrowsingHistoryService _browsingHistoryService;
    private int _extensionsInitialized;
    private bool _isClosing;
    private CancellationTokenSource? _startupCts;

    public MainWindow(
        MainViewModel viewModel,
        BrowserOptions browserOptions,
        WebView2EnvironmentService environmentService,
        IDialogService dialogService,
        ITabHostService tabHostService,
        PermissionMemoryStore permissionStore,
        BrowserExtensionService extensionService,
        BrowsingDataClearService browsingDataClearService,
        UserScriptService userScriptService,
        UserScriptBridge userScriptBridge,
        TabSleepService tabSleepService,
        IDownloadService downloadService,
        IBrowsingHistoryService browsingHistoryService)
    {
        _viewModel = viewModel;
        _browserOptions = browserOptions;
        _environmentService = environmentService;
        _dialogService = dialogService;
        _tabHostService = tabHostService;
        _permissionStore = permissionStore;
        _extensionService = extensionService;
        _browsingDataClearService = browsingDataClearService;
        _userScriptService = userScriptService;
        _userScriptBridge = userScriptBridge;
        _tabSleepService = tabSleepService;
        _downloadService = downloadService;
        _browsingHistoryService = browsingHistoryService;

        InitializeComponent();
        DataContext = _viewModel;

        _viewModel.CloseTabRequested += OnCloseTabRequested;
        _viewModel.Favorites.PropertyChanged += OnFavoritesPropertyChanged;
        _viewModel.Extensions.PropertyChanged += OnExtensionsPropertyChanged;
        _viewModel.UserScripts.PropertyChanged += OnUserScriptsPropertyChanged;
        _viewModel.Downloads.PropertyChanged += OnDownloadsPropertyChanged;
    }

    public CoreWebView2Environment? WebViewEnvironment { get; private set; }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        _startupCts = new CancellationTokenSource();
        var token = _startupCts.Token;

        try
        {
            _viewModel.IsToolbarEnabled = false;
            await _permissionStore.LoadAsync();
            WebViewEnvironment = await _environmentService.GetAsync();
            token.ThrowIfCancellationRequested();

            if (_isClosing)
                return;

            await _viewModel.InitializeAsync();

            _viewModel.IsToolbarEnabled = true;
            _viewModel.NotifyTabReadyChanged();
            _tabSleepService.Start(Dispatcher);

            if (_viewModel.Favorites.IsPanelOpen)
                favoritesColumn.Width = new GridLength(FavoritesPanelWidth);

            if (_viewModel.UserScripts.IsPanelOpen)
                userScriptsColumn.Width = new GridLength(UserScriptsPanelWidth);

            if (_viewModel.Extensions.IsPanelOpen)
                extensionsColumn.Width = new GridLength(ExtensionsPanelWidth);

            if (_viewModel.Downloads.IsPanelOpen)
                downloadsPanel.Height = DownloadsPanelHeight;
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            if (!_isClosing)
                _dialogService.ShowError(
                    $"无法初始化 WebView2 环境。\n\n{ex.Message}",
                    "初始化失败");
        }
        finally
        {
            _startupCts?.Dispose();
            _startupCts = null;
        }
    }

    private async void Window_Closing(object? sender, CancelEventArgs e)
    {
        _isClosing = true;
        _tabSleepService.Stop();
        _startupCts?.Cancel();

        _viewModel.CloseTabRequested -= OnCloseTabRequested;
        _viewModel.Favorites.PropertyChanged -= OnFavoritesPropertyChanged;
        _viewModel.Extensions.PropertyChanged -= OnExtensionsPropertyChanged;
        _viewModel.Downloads.PropertyChanged -= OnDownloadsPropertyChanged;
        _viewModel.Extensions.Detach();

        await _tabSleepService.FlushAsync();

        foreach (var host in _tabHostService.GetAllHosts())
            host.Shutdown();

        await _permissionStore.DisposeAsync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.T)
        {
            _viewModel.NewTabCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.W)
        {
            _viewModel.CloseTabCommand.Execute(_viewModel.SelectedTab);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.Tab)
        {
            _viewModel.SelectNextTab();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.H)
        {
            _viewModel.History.TogglePageCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F
            && _viewModel.IsBrowserContentVisible)
        {
            _viewModel.ShowFindCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.R
            && _viewModel.IsBrowserContentVisible)
        {
            _viewModel.UserScripts.ReloadAllOpenTabsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.OemComma)
        {
            _viewModel.Settings.TogglePageCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.History.IsPageOpen)
        {
            _viewModel.History.ClosePageCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && _viewModel.Settings.IsPageOpen)
        {
            _viewModel.Settings.ClosePageCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F12)
        {
            _viewModel.OpenDevToolsCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void AddressBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (sender is System.Windows.Controls.TextBox addressBox)
            {
                // Commit pending edit before Navigate (binding uses LostFocus).
                addressBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty)?.UpdateSource();
            }

            _viewModel.NavigateCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void AddressBar_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.TextBox textBox)
            textBox.SelectAll();
    }

    private void TabHost_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TabWebViewHost host)
            RegisterHost(host);
    }

    public void RegisterHost(TabWebViewHost host)
    {
        host.DialogService = _dialogService;
        host.DownloadService = _downloadService;
        host.BrowsingHistoryService = _browsingHistoryService;
        host.PermissionStore = _permissionStore;
        host.UserScriptService = _userScriptService;
        host.UserScriptBridge = _userScriptBridge;
        host.BrowserOptions = _browserOptions;
        host.TabHostCallbacks = _viewModel;
        host.ProfileReady += OnHostProfileReady;

        if (host.Tab == null)
            return;

        _tabHostService.Register(host.Tab.TabId, host);

        if (host.Tab.IsSelected && (host.Tab.IsSleeping || host.Tab.IsLightSuspended))
            _ = host.WakeAsync();
    }

    private async void OnHostProfileReady(object? sender, CoreWebView2Profile profile)
    {
        if (sender is TabWebViewHost host)
            host.ProfileReady -= OnHostProfileReady;

        _browsingDataClearService.SetProfile(profile);

        if (Interlocked.CompareExchange(ref _extensionsInitialized, 1, 0) != 0)
            return;

        try
        {
            await _extensionService.InitializeAsync(profile);
            _viewModel.Extensions.RefreshFromService();
        }
        catch (Exception ex)
        {
            if (!_isClosing)
                _dialogService.ShowWarning(
                    $"扩展程序服务初始化失败，部分功能可能不可用。\n\n{ex.Message}",
                    "扩展程序");
        }
    }

    private void OnCloseTabRequested(BrowserTabViewModel tab)
    {
        if (_tabHostService.GetHost(tab.TabId) is { } host)
        {
            host.Shutdown();
            _tabHostService.Unregister(tab.TabId);
        }
    }

    private void OnFavoritesPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FavoritesViewModel.IsPanelOpen))
            AnimateFavoritesPanel(_viewModel.Favorites.IsPanelOpen);
    }

    private void AnimateFavoritesPanel(bool isOpen)
    {
        AnimatePanelColumn(favoritesColumn, isOpen ? FavoritesPanelWidth : 0);
    }

    private void OnExtensionsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ExtensionsViewModel.IsPanelOpen))
            AnimateExtensionsPanel(_viewModel.Extensions.IsPanelOpen);
    }

    private void OnUserScriptsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(UserScriptsViewModel.IsPanelOpen))
            AnimateUserScriptsPanel(_viewModel.UserScripts.IsPanelOpen);
    }

    private void AnimateUserScriptsPanel(bool isOpen)
    {
        AnimatePanelColumn(userScriptsColumn, isOpen ? UserScriptsPanelWidth : 0);
    }

    private void AnimateExtensionsPanel(bool isOpen)
    {
        AnimatePanelColumn(extensionsColumn, isOpen ? ExtensionsPanelWidth : 0);
    }

    private void OnDownloadsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(DownloadsViewModel.IsPanelOpen))
            AnimateDownloadsPanel(_viewModel.Downloads.IsPanelOpen);
    }

    private void AnimateDownloadsPanel(bool isOpen)
    {
        var animation = new DoubleAnimation
        {
            From = downloadsPanel.ActualHeight > 0 ? downloadsPanel.ActualHeight : downloadsPanel.Height,
            To = isOpen ? DownloadsPanelHeight : 0,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        downloadsPanel.BeginAnimation(FrameworkElement.HeightProperty, animation);
    }

    private static void AnimatePanelColumn(System.Windows.Controls.ColumnDefinition column, double targetWidth)
    {
        var animation = new GridLengthAnimation
        {
            From = column.Width,
            To = new GridLength(targetWidth),
            Duration = TimeSpan.FromMilliseconds(200)
        };
        column.BeginAnimation(System.Windows.Controls.ColumnDefinition.WidthProperty, animation);
    }
}