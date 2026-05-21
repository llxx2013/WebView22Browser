using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Async;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Controls;

/// <summary>
/// Core tab host: dependency properties, DI configuration, VM command wiring, and loading UI.
/// </summary>
public partial class TabWebViewHost : UserControl, ITabWebViewHost
{
    public static readonly DependencyProperty TabProperty =
        DependencyProperty.Register(nameof(Tab), typeof(BrowserTabViewModel), typeof(TabWebViewHost),
            new PropertyMetadata(null, OnTabChanged));

    public static readonly DependencyProperty EnvironmentProperty =
        DependencyProperty.Register(nameof(Environment), typeof(CoreWebView2Environment), typeof(TabWebViewHost),
            new PropertyMetadata(null, OnEnvironmentChanged));

    private bool _isFindPending;

    public TabWebViewHost()
    {
        InitializeComponent();
        AddHandler(PreviewMouseDownEvent, (MouseButtonEventHandler)OnPreviewMouseDown, true);
        AddHandler(PreviewMouseUpEvent, (MouseButtonEventHandler)OnPreviewMouseUp, true);
    }

    public BrowserTabViewModel? Tab
    {
        get => (BrowserTabViewModel?)GetValue(TabProperty);
        set => SetValue(TabProperty, value);
    }

    public CoreWebView2Environment? Environment
    {
        get => (CoreWebView2Environment?)GetValue(EnvironmentProperty);
        set => SetValue(EnvironmentProperty, value);
    }

    public IDialogService? DialogService { get; private set; }

    public IDownloadService? DownloadService { get; private set; }

    public IBrowsingHistoryService? BrowsingHistoryService { get; private set; }

    public PermissionMemoryStore? PermissionStore { get; private set; }

    public UserScriptService? UserScriptService { get; private set; }

    public UserScriptBridge? UserScriptBridge { get; private set; }

    public BrowserOptions? BrowserOptions { get; private set; }

    public ITabHostCallbacks? TabHostCallbacks { get; private set; }

    /// <summary>
    /// Injects host dependencies from <see cref="MainWindow.RegisterHost"/> (DI composition root).
    /// Must run before WebView2 lifecycle methods use services.
    /// </summary>
    public void ConfigureHost(
        IDialogService dialogService,
        IDownloadService downloadService,
        IBrowsingHistoryService browsingHistoryService,
        PermissionMemoryStore permissionStore,
        UserScriptService userScriptService,
        UserScriptBridge userScriptBridge,
        BrowserOptions browserOptions,
        ITabHostCallbacks tabHostCallbacks)
    {
        ArgumentNullException.ThrowIfNull(dialogService);
        ArgumentNullException.ThrowIfNull(downloadService);
        ArgumentNullException.ThrowIfNull(browsingHistoryService);
        ArgumentNullException.ThrowIfNull(permissionStore);
        ArgumentNullException.ThrowIfNull(userScriptService);
        ArgumentNullException.ThrowIfNull(userScriptBridge);
        ArgumentNullException.ThrowIfNull(browserOptions);
        ArgumentNullException.ThrowIfNull(tabHostCallbacks);

        DialogService = dialogService;
        DownloadService = downloadService;
        BrowsingHistoryService = browsingHistoryService;
        PermissionStore = permissionStore;
        UserScriptService = userScriptService;
        UserScriptBridge = userScriptBridge;
        BrowserOptions = browserOptions;
        TabHostCallbacks = tabHostCallbacks;
        _isHostConfigured = true;

        if (IsLoaded)
            FireAndForget.Run(TryBeginHostLifecycleAsync, ReportHostLifecycleFailure);
    }

    public CoreWebView2? CoreWebView2 => webView.CoreWebView2;

    public event Action<BrowserTabViewModel>? InitializationFailed;

    public event EventHandler<CoreWebView2Profile>? ProfileReady;

    private static void OnTabChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabWebViewHost host)
            host.OnTabSubscriptionChanged((BrowserTabViewModel?)e.OldValue, (BrowserTabViewModel?)e.NewValue);
    }

    private static void OnEnvironmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TabWebViewHost host && host.IsLoaded && host._isHostConfigured)
            _ = host.InitializeAsync();
    }

    private void OnTabSubscriptionChanged(BrowserTabViewModel? oldTab, BrowserTabViewModel? newTab)
    {
        if (oldTab != null)
        {
            oldTab.PropertyChanged -= OnTabPropertyChanged;
            oldTab.NavigateRequested -= OnNavigateRequested;
            oldTab.GoBackRequested -= OnGoBackRequested;
            oldTab.GoForwardRequested -= OnGoForwardRequested;
            oldTab.ReloadRequested -= OnReloadRequested;
            oldTab.StopRequested -= OnStopRequested;
            oldTab.OpenDevToolsRequested -= OnOpenDevToolsRequested;
            oldTab.ShowFindRequested -= OnShowFindRequested;
        }

        if (newTab != null)
        {
            newTab.PropertyChanged += OnTabPropertyChanged;
            newTab.NavigateRequested += OnNavigateRequested;
            newTab.GoBackRequested += OnGoBackRequested;
            newTab.GoForwardRequested += OnGoForwardRequested;
            newTab.ReloadRequested += OnReloadRequested;
            newTab.StopRequested += OnStopRequested;
            newTab.OpenDevToolsRequested += OnOpenDevToolsRequested;
            newTab.ShowFindRequested += OnShowFindRequested;
        }

        if (!_isHostConfigured || !IsLoaded)
            return;

        if (Environment != null && newTab != null && !newTab.IsSleeping)
            _ = InitializeAsync();
        else if (newTab is { IsSelected: true } && (newTab.IsSleeping || newTab.IsLightSuspended))
            _ = WakeAsync();
    }

    private void OnTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!_isHostConfigured
            || e.PropertyName != nameof(BrowserTabViewModel.IsSelected)
            || Tab == null
            || !Tab.IsSelected)
            return;

        if ((Tab.IsSleeping || Tab.IsLightSuspended) && IsLoaded)
            _ = WakeAsync();
    }

    private void OnNavigateRequested(string uri)
    {
        if (_isPermanentClose || webView.CoreWebView2 == null)
            return;

        Tab?.TouchActivity();
        webView.CoreWebView2.Navigate(uri);
    }

    private void OnGoBackRequested()
    {
        if (webView.CanGoBack)
            webView.GoBack();
    }

    private void OnGoForwardRequested()
    {
        if (webView.CanGoForward)
            webView.GoForward();
    }

    private void OnReloadRequested()
    {
        if (webView.CoreWebView2 == null || Tab == null)
            return;

        Tab.IsLoading = true;
        Tab.LoadingStatus = "正在刷新...";
        UpdateContentVisibility();
        webView.CoreWebView2.Reload();
    }

    private void OnStopRequested()
    {
        if (webView.CoreWebView2 == null || Tab == null)
            return;

        Tab.SuppressNavigationError = true;
        webView.CoreWebView2.Stop();
        Tab.IsLoading = false;
        UpdateContentVisibility();
    }

    private void OnOpenDevToolsRequested() => webView.CoreWebView2?.OpenDevToolsWindow();

    private void OnShowFindRequested() =>
        FireAndForget.Run(OnShowFindRequestedAsync);

    private async Task OnShowFindRequestedAsync()
    {
        if (_isFindPending || _hasShutdown || _isPermanentClose || Tab == null)
            return;

        _isFindPending = true;
        try
        {
            if (Tab is { IsSleeping: true } or { IsLightSuspended: true })
                await WakeAsync();

            if (webView.CoreWebView2 is not { Find: { } find, Environment: { } environment })
                return;

            await find.StartAsync(environment.CreateFindOptions());
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WebView22Browser] Find.StartAsync failed: {ex.Message}");
        }
        finally
        {
            _isFindPending = false;
        }
    }

    private void ShowSkeleton(string? status)
    {
        if (Tab == null)
            return;

        if (!string.IsNullOrEmpty(status))
            Tab.LoadingStatus = status;

        if (!Tab.IsSleeping)
            Tab.IsLoading = true;

        UpdateContentVisibility();
    }

    private void RevealContent()
    {
        _contentRevealed = true;
        UpdateContentVisibility();
    }

    private void EnterSleepPlaceholder()
    {
        _contentRevealed = false;
        if (Tab != null)
        {
            Tab.IsLoading = false;
            Tab.LoadingStatus = string.Empty;
        }

        UpdateContentVisibility();
    }

    private void UpdateContentVisibility()
    {
        if (Tab == null)
        {
            webView.Visibility = Visibility.Collapsed;
            skeletonPanel.Visibility = Visibility.Collapsed;
            navigationBarPanel.Visibility = Visibility.Collapsed;
            return;
        }

        var preReveal = !_contentRevealed || Tab.IsSleeping || Tab.IsLightSuspended;
        webView.Visibility = preReveal ? Visibility.Collapsed : Visibility.Visible;
        skeletonPanel.Visibility = preReveal ? Visibility.Visible : Visibility.Collapsed;
        navigationBarPanel.Visibility = !preReveal && Tab.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        navigationProgress.IsIndeterminate = Tab.IsLoading;
        skeletonProgress.IsIndeterminate = Tab.IsLoading;
    }
}