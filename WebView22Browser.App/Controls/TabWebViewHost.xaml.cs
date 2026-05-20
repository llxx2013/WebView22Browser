using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.App.WebView2;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Controls;

public partial class TabWebViewHost : UserControl
{
    public static readonly DependencyProperty TabProperty =
        DependencyProperty.Register(nameof(Tab), typeof(BrowserTabViewModel), typeof(TabWebViewHost),
            new PropertyMetadata(null, OnTabChanged));

    public static readonly DependencyProperty EnvironmentProperty =
        DependencyProperty.Register(nameof(Environment), typeof(CoreWebView2Environment), typeof(TabWebViewHost),
            new PropertyMetadata(null, OnEnvironmentChanged));

    private const int MaxProcessRecoveryAttempts = 3;

    private bool _isPermanentClose;
    private int _processRecoveryAttempts;
    private bool _processRecoveryExhausted;
    private bool _hasShutdown;
    private bool _isWired;
    private bool _middleClickOpensNewTab;
    private CancellationTokenSource? _initCts;
    private CancellationTokenSource? _middleClickResetCts;
    private TabNavigationHistory? _navigationHistory;
    private bool _isRestoringHistory;
    private bool _contentRevealed;
    private TaskCompletionSource<bool>? _navigationWaiter;
    private bool _securityDevToolsEnabled;
    private CoreWebView2DevToolsProtocolEventReceiver? _securityEventReceiver;
    private EventHandler<CoreWebView2DevToolsProtocolEventReceivedEventArgs>? _securityStateHandler;

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

    public IDialogService? DialogService { get; set; }

    public IDownloadService? DownloadService { get; set; }

    public IBrowsingHistoryService? BrowsingHistoryService { get; set; }

    public PermissionMemoryStore? PermissionStore { get; set; }

    public UserScriptService? UserScriptService { get; set; }

    public UserScriptBridge? UserScriptBridge { get; set; }

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
        if (d is TabWebViewHost host && host.IsLoaded)
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
        }

        if (IsLoaded && Environment != null && newTab != null && !newTab.IsSleeping)
            _ = InitializeAsync();
        else if (IsLoaded && newTab is { IsSelected: true } && (newTab.IsSleeping || newTab.IsLightSuspended))
            _ = WakeAsync();
    }

    private void OnTabPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(BrowserTabViewModel.IsSelected) || Tab == null || !Tab.IsSelected)
            return;

        if ((Tab.IsSleeping || Tab.IsLightSuspended) && IsLoaded)
            _ = WakeAsync();
    }

    private async void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (Tab == null)
            return;

        if (Tab.IsSleeping || Tab.IsLightSuspended)
        {
            if (Tab.IsSelected)
                await WakeAsync();
            else
                EnterSleepPlaceholder();
            return;
        }

        if (Environment != null)
            await InitializeAsync();
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (Tab?.IsSleeping == true)
            return;

        Shutdown();
    }

    public void Shutdown()
    {
        if (_hasShutdown)
            return;

        _hasShutdown = true;
        _isPermanentClose = true;
        _initCts?.Cancel();

        if (Tab != null)
            DownloadService?.DetachTab(Tab);

        UnwireEvents(resetPlaybackState: true);

        if (webView.CoreWebView2 != null && Tab?.IsLoading == true)
        {
            Tab.SuppressNavigationError = true;
            webView.CoreWebView2.Stop();
        }

        try
        {
            webView.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // Sleeping tabs may have already disposed the control in SuspendAsync.
        }
    }

    public Task ReduceMemoryAsync()
    {
        if (_hasShutdown || _isPermanentClose || Tab == null || Tab.IsSleeping)
            return Task.CompletedTask;

        if (!Tab.IsWebViewReady || webView.CoreWebView2 == null)
            return Task.CompletedTask;

        webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
        Tab.IsMemoryReduced = true;
        return Task.CompletedTask;
    }

    public async Task SuspendLightAsync()
    {
        if (_hasShutdown || _isPermanentClose || Tab == null || Tab.IsSleeping || Tab.IsLightSuspended)
            return;

        if (!Tab.IsWebViewReady || webView.CoreWebView2 == null)
            return;

        SyncTabStateFromWebView();
        var suspended = await webView.CoreWebView2.TrySuspendAsync();
        if (!suspended)
            return;

        Tab.IsLightSuspended = true;
        EnterSleepPlaceholder();
    }

    public Task SuspendAsync()
    {
        if (_hasShutdown || _isPermanentClose || Tab == null || Tab.IsSleeping)
            return Task.CompletedTask;

        if (!Tab.IsWebViewReady || webView.CoreWebView2 == null)
            return Task.CompletedTask;

        _initCts?.Cancel();
        SyncTabStateFromWebView();
        FreezeNavigationHistory();
        UnwireEvents(resetPlaybackState: false);

        if (Tab.IsLoading)
        {
            Tab.SuppressNavigationError = true;
            webView.CoreWebView2.Stop();
            Tab.IsLoading = false;
        }

        Tab.IsLightSuspended = false;
        Tab.IsMemoryReduced = false;
        RecreateWebViewControl();

        Tab.IsSleeping = true;
        Tab.IsWebViewReady = false;
        EnterSleepPlaceholder();
        return Task.CompletedTask;
    }

    public async Task WakeAsync()
    {
        if (_hasShutdown || _isPermanentClose || Tab == null)
            return;

        if (Tab.IsLightSuspended && !Tab.IsSleeping && webView.CoreWebView2 != null)
        {
            Tab.IsLightSuspended = false;
            Tab.IsMemoryReduced = false;
            webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
            webView.CoreWebView2.Resume();
            RevealContent();
            return;
        }

        if (!Tab.IsSleeping)
            return;

        Tab.IsSleeping = false;
        Tab.IsLightSuspended = false;
        Tab.IsMemoryReduced = false;
        _hasShutdown = false;
        await InitializeAsync();
    }

    public TabNavigationSnapshot? TryGetHistorySnapshot()
    {
        if (Tab == null)
            return null;

        if (Tab.IsSleeping && Tab.FrozenHistoryEntries is { Count: > 0 })
            return new TabNavigationSnapshot(Tab.FrozenHistoryEntries, Tab.FrozenHistoryIndex);

        if (!Tab.IsWebViewReady || webView.CoreWebView2 == null)
            return null;

        SyncTabStateFromWebView();
        return NavigationHistory.GetSnapshot();
    }

    private async Task InitializeAsync()
    {
        if (Tab == null || Environment == null || _isPermanentClose)
            return;

        if (Tab.IsWebViewReady && webView.CoreWebView2 != null)
            return;

        _initCts?.Cancel();
        _initCts = new CancellationTokenSource();
        var token = _initCts.Token;

        try
        {
            _contentRevealed = false;
            Tab.IsLoading = true;
            ShowSkeleton("正在初始化 WebView2...");

            var initTask = webView.EnsureCoreWebView2Async(Environment);
            var completed = await Task.WhenAny(initTask, Task.Delay(Timeout.Infinite, token));
            if (completed != initTask)
                return;

            await initTask;
            token.ThrowIfCancellationRequested();

            if (_isPermanentClose || Tab == null)
                return;

            ProfileReady?.Invoke(this, webView.CoreWebView2!.Profile);
            WireEvents();
            await ApplyUserScriptsAndBridgeAsync();
            Tab.IsWebViewReady = true;
            Tab.IsSleeping = false;
            _processRecoveryAttempts = 0;

            if (Application.Current.MainWindow?.DataContext is MainViewModel main)
                main.NotifyTabReadyChanged();

            if (Tab.FrozenHistoryEntries is { Count: > 0 })
                await RestoreFrozenHistoryAsync();
            else
                await NavigateToStartUriAsync();

            if (_isPermanentClose || Tab == null)
                return;

            Tab.IsLoading = false;
            RevealContent();
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            if (!_isPermanentClose && Tab != null)
            {
                DialogService?.ShowError(
                    $"无法初始化 WebView2。请安装 Microsoft Edge WebView2 Runtime 后重试。\n\n{ex.Message}",
                    "WebView2 初始化失败");
                InitializationFailed?.Invoke(Tab);
            }
        }
        finally
        {
            _initCts?.Dispose();
            _initCts = null;
        }
    }

    private void WireEvents()
    {
        if (_isWired || webView.CoreWebView2 == null || Tab == null)
            return;

        var core = webView.CoreWebView2;
        core.Settings.AreDevToolsEnabled = true;
        _ = core.ClearServerCertificateErrorActionsAsync();

        core.NavigationStarting += OnNavigationStarting;
        core.NavigationCompleted += OnNavigationCompleted;
        core.SourceChanged += OnSourceChanged;
        core.HistoryChanged += OnHistoryChanged;
        core.DocumentTitleChanged += OnDocumentTitleChanged;
        core.ContentLoading += OnContentLoading;
        core.NewWindowRequested += OnNewWindowRequested;
        core.ContextMenuRequested += OnContextMenuRequested;
        core.DownloadStarting += OnDownloadStarting;
        core.ServerCertificateErrorDetected += OnServerCertificateErrorDetected;
        core.PermissionRequested += OnPermissionRequested;
        core.ProcessFailed += OnProcessFailed;
        core.IsDocumentPlayingAudioChanged += OnIsDocumentPlayingAudioChanged;

        if (Tab != null)
            Tab.IsPlayingAudio = core.IsDocumentPlayingAudio;

        _ = EnableSecurityMonitoringAsync(core);

        _isWired = true;
    }

    private void UnwireEvents(bool resetPlaybackState = false)
    {
        if (!_isWired || webView.CoreWebView2 == null)
            return;

        var core = webView.CoreWebView2;
        core.NavigationStarting -= OnNavigationStarting;
        core.NavigationCompleted -= OnNavigationCompleted;
        core.SourceChanged -= OnSourceChanged;
        core.HistoryChanged -= OnHistoryChanged;
        core.DocumentTitleChanged -= OnDocumentTitleChanged;
        core.ContentLoading -= OnContentLoading;
        core.NewWindowRequested -= OnNewWindowRequested;
        core.ContextMenuRequested -= OnContextMenuRequested;
        core.DownloadStarting -= OnDownloadStarting;
        core.ServerCertificateErrorDetected -= OnServerCertificateErrorDetected;
        core.PermissionRequested -= OnPermissionRequested;
        core.ProcessFailed -= OnProcessFailed;
        core.IsDocumentPlayingAudioChanged -= OnIsDocumentPlayingAudioChanged;

        if (Tab != null && resetPlaybackState)
            Tab.IsPlayingAudio = false;

        if (_securityEventReceiver != null && _securityStateHandler != null)
            _securityEventReceiver.DevToolsProtocolEventReceived -= _securityStateHandler;

        _securityDevToolsEnabled = false;
        _securityEventReceiver = null;
        _securityStateHandler = null;

        _middleClickOpensNewTab = false;
        _middleClickResetCts?.Cancel();
        _middleClickResetCts = null;

        if (Tab != null)
            Tab.IsWebViewReady = false;

        UserScriptBridge?.Unwire(core);
        UserScriptService?.UnregisterWebView(core);

        _isWired = false;
    }

    private async Task ApplyUserScriptsAndBridgeAsync()
    {
        if (webView.CoreWebView2 is not { } core)
            return;

        UserScriptBridge?.ClearMenuCommands(core);
        UserScriptBridge?.Wire(core);

        if (UserScriptService != null)
            await UserScriptService.ApplyToWebViewAsync(core);
    }

    private void SyncTabStateFromWebView()
    {
        if (Tab == null)
            return;

        if (webView.CoreWebView2 != null)
        {
            var source = webView.Source?.ToString();
            if (string.IsNullOrEmpty(source))
                source = webView.CoreWebView2.Source;
            if (!string.IsNullOrEmpty(source))
                Tab.Address = source;

            var documentTitle = webView.CoreWebView2.DocumentTitle;
            if (!string.IsNullOrWhiteSpace(documentTitle))
                Tab.Title = documentTitle;
        }

        UpdateHistoryState();
    }

    private TabNavigationHistory NavigationHistory =>
        _navigationHistory ??= CreateNavigationHistory();

    private TabNavigationHistory CreateNavigationHistory()
    {
        var maxEntries = 50;
        if (App.Services.GetService<BrowserOptions>() is { } options)
            maxEntries = options.TabHistoryMaxEntries;

        return new TabNavigationHistory(maxEntries);
    }

    private void FreezeNavigationHistory()
    {
        if (Tab == null)
            return;

        var snapshot = NavigationHistory.GetSnapshot();
        Tab.FrozenHistoryEntries = snapshot.Entries;
        Tab.FrozenHistoryIndex = snapshot.CurrentIndex;
    }

    private async Task NavigateToStartUriAsync()
    {
        if (Tab == null || webView.CoreWebView2 == null)
            return;

        var startUri = !string.IsNullOrWhiteSpace(Tab.Address)
            ? Tab.Address
            : Tab.InitialUri;
        if (string.IsNullOrEmpty(startUri))
            return;

        webView.CoreWebView2.Navigate(startUri);
        await WaitForNavigationCompletedAsync();
    }

    private async Task RestoreFrozenHistoryAsync()
    {
        if (Tab == null || webView.CoreWebView2 == null)
            return;

        var entries = Tab.FrozenHistoryEntries!;
        var targetIndex = Tab.FrozenHistoryIndex;
        Tab.ClearFrozenHistory();

        _isRestoringHistory = true;
        try
        {
            var steps = TabHistoryRestorer.BuildSteps(entries, targetIndex);
            foreach (var step in steps)
            {
                if (step.Kind == TabHistoryRestoreActionKind.Navigate)
                {
                    webView.CoreWebView2.Navigate(step.Uri!);
                    await WaitForNavigationCompletedAsync();
                }
                else
                {
                    webView.GoBack();
                    await WaitForNavigationCompletedAsync();
                }
            }

            NavigationHistory.RestoreSnapshot(entries, targetIndex);
            SyncTabStateFromWebView();
        }
        finally
        {
            _isRestoringHistory = false;
        }
    }

    private Task WaitForNavigationCompletedAsync()
    {
        _navigationWaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _navigationWaiter.Task;
    }

    private void CompleteNavigationWait(bool success)
    {
        var waiter = _navigationWaiter;
        if (waiter == null)
            return;

        _navigationWaiter = null;
        if (success)
            waiter.TrySetResult(true);
        else
            waiter.TrySetResult(false);
    }

    private void RecordNavigationIfNeeded()
    {
        if (_isRestoringHistory || Tab == null || webView.CoreWebView2 == null)
            return;

        var uri = webView.Source?.ToString();
        if (string.IsNullOrEmpty(uri))
            uri = webView.CoreWebView2.Source;

        if (!string.IsNullOrEmpty(uri))
            NavigationHistory.RecordNavigation(uri);
    }

    private async Task RecordBrowsingHistoryAsync()
    {
        if (_isRestoringHistory || Tab == null || webView.CoreWebView2 == null || BrowsingHistoryService == null)
            return;

        var uri = webView.Source?.ToString();
        if (string.IsNullOrEmpty(uri))
            uri = webView.CoreWebView2.Source;

        if (string.IsNullOrEmpty(uri))
            return;

        try
        {
            await BrowsingHistoryService.RecordVisitAsync(uri, Tab.Title);
        }
        catch (Exception ex)
        {
            // History persistence should not break navigation.
            Debug.WriteLine($"Browsing history record failed: {ex}");
        }
    }

    private void OnIsDocumentPlayingAudioChanged(object? sender, object e)
    {
        if (_isPermanentClose || Tab == null || webView.CoreWebView2 == null)
            return;

        Tab.IsPlayingAudio = webView.CoreWebView2.IsDocumentPlayingAudio;
        if (Tab.IsPlayingAudio)
            Tab.TouchActivity();
    }

    private void RecreateWebViewControl()
    {
        var parent = webView.Parent as Panel;
        var index = parent?.Children.IndexOf(webView) ?? -1;
        if (parent != null && index >= 0)
            parent.Children.Remove(webView);

        try
        {
            webView.Dispose();
        }
        catch (ObjectDisposedException)
        {
            // WebView2 may already have been disposed by the caller.
        }

        webView = new Microsoft.Web.WebView2.Wpf.WebView2 { Visibility = Visibility.Collapsed };

        if (parent != null && index >= 0)
            parent.Children.Insert(index, webView);
        else if (Content is Panel fallbackParent)
            fallbackParent.Children.Insert(0, webView);

        _contentRevealed = false;
        UpdateContentVisibility();
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

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_isPermanentClose || Tab == null)
            return;

        if (webView.CoreWebView2 is { } core)
            UserScriptBridge?.ClearMenuCommands(core);

        Tab.TouchActivity();
        Tab.IsLoading = true;
        Tab.LoadingStatus = "正在连接...";
        if (_contentRevealed)
            UpdateContentVisibility();
        else
            ShowSkeleton(Tab.LoadingStatus);
        UpdateHistoryState();
    }

    private void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (_isPermanentClose || Tab == null)
            return;

        if (_isRestoringHistory)
        {
            CompleteNavigationWait(e.IsSuccess);
            return;
        }

        Tab.IsLoading = false;
        UpdateContentVisibility();
        UpdateHistoryState();

        if (e.IsSuccess)
        {
            RecordNavigationIfNeeded();
            _ = RecordBrowsingHistoryAsync();
        }
        else
        {
            Tab.SecurityState = AddressBarSecurityState.Unknown;
        }

        CompleteNavigationWait(e.IsSuccess);

        if (!e.IsSuccess && DialogService != null)
        {
            var browserStatus = WebView2ErrorMapper.ToBrowser(e.WebErrorStatus);
            var decision = NavigationErrorPolicy.Evaluate(
                browserStatus,
                Tab.SuppressNavigationError,
                _isPermanentClose);

            if (decision.ResetSuppressFlag)
                Tab.SuppressNavigationError = false;

            if (decision.ShouldShow)
            {
                var message = NavigationErrorFormatter.Format(browserStatus);
                DialogService.ShowWarning($"导航失败: {message}", "导航错误");
            }
        }
        else if (Tab.SuppressNavigationError)
        {
            Tab.SuppressNavigationError = false;
        }
    }

    private void OnSourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        if (_isPermanentClose || Tab == null || webView.Source == null)
            return;

        var source = webView.Source.ToString();
        Tab.Address = source;
        Tab.TouchActivity();

        if (!_securityDevToolsEnabled || !SecurityStateDevToolsParser.UsesHttpSecurityScheme(source))
            ApplySchemeSecurityFallback();
    }

    private void OnHistoryChanged(object? sender, object e) => UpdateHistoryState();

    private void OnDocumentTitleChanged(object? sender, object e)
    {
        if (_isPermanentClose || Tab == null || webView.CoreWebView2 == null)
            return;

        Tab.Title = string.IsNullOrWhiteSpace(webView.CoreWebView2.DocumentTitle)
            ? "新标签页"
            : webView.CoreWebView2.DocumentTitle;

        if (Tab.IsSelected && Application.Current.MainWindow?.DataContext is MainViewModel main)
            main.UpdateWindowTitle();
    }

    private void OnContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
    {
        if (_isPermanentClose || Tab == null || e.IsErrorPage)
            return;

        Tab.LoadingStatus = "正在加载页面内容...";
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (_isPermanentClose || Tab == null)
            return;

        e.Handled = true;
        if (Application.Current.MainWindow?.DataContext is not MainViewModel main)
            return;

        var openInNewTab = e.IsUserInitiated
            || NewTabGestureDetector.IsControlClick()
            || ConsumeMiddleClickNewTab();
        main.HandleNavigation(e.Uri, Tab, openInNewTab);
    }

    private void OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        _middleClickResetCts?.Cancel();
        _middleClickOpensNewTab = true;
    }

    private void OnPreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle)
            return;

        ScheduleMiddleClickReset();
    }

    private void ScheduleMiddleClickReset()
    {
        _middleClickResetCts?.Cancel();
        _middleClickResetCts = new CancellationTokenSource();
        var token = _middleClickResetCts.Token;

        _ = Dispatcher.BeginInvoke(async () =>
        {
            try
            {
                await Task.Delay(500, token);
                _middleClickOpensNewTab = false;
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
        }, DispatcherPriority.Background);
    }

    private bool ConsumeMiddleClickNewTab()
    {
        if (!_middleClickOpensNewTab)
            return false;

        _middleClickOpensNewTab = false;
        _middleClickResetCts?.Cancel();
        return true;
    }

    private void OnContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs e)
    {
        if (_isPermanentClose || Tab == null || webView.CoreWebView2 == null)
            return;

        var target = e.ContextMenuTarget;
        if (!target.HasLinkUri || string.IsNullOrEmpty(target.LinkUri))
            return;

        WebView2ContextMenuHelper.RemoveDefaultOpenInNewTabItems(e.MenuItems);

        var linkUri = target.LinkUri;
        var sourceTab = Tab;
        var openInNewTabItem = webView.CoreWebView2.Environment.CreateContextMenuItem(
            "在新标签页中打开",
            null,
            CoreWebView2ContextMenuItemKind.Command);

        openInNewTabItem.CustomItemSelected += (_, _) =>
        {
            if (Application.Current.MainWindow?.DataContext is MainViewModel main)
                main.HandleNavigation(linkUri, sourceTab, openInNewTab: true);
        };

        e.MenuItems.Insert(0, openInNewTabItem);
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        if (DownloadService == null || Tab == null)
            return;

        DownloadService.HandleDownloadStarting(e, Tab);
    }

    private void OnServerCertificateErrorDetected(object? sender, CoreWebView2ServerCertificateErrorDetectedEventArgs e)
    {
        if (DialogService == null)
        {
            e.Action = CoreWebView2ServerCertificateErrorAction.Cancel;
            if (Tab != null)
                Tab.SecurityState = AddressBarSecurityState.Dangerous;
            return;
        }

        // WebView2 only exposes AlwaysAllow (session-scoped cache) or Cancel — no per-navigation Allow.
        var allow = DialogService.PromptCertificateError(e.RequestUri, e.ErrorStatus.ToString());
        e.Action = allow
            ? CoreWebView2ServerCertificateErrorAction.AlwaysAllow
            : CoreWebView2ServerCertificateErrorAction.Cancel;

        if (!allow && Tab != null)
            Tab.SecurityState = AddressBarSecurityState.Dangerous;
    }

    private async Task EnableSecurityMonitoringAsync(CoreWebView2 core)
    {
        if (_securityDevToolsEnabled || Tab == null)
            return;

        try
        {
            await core.CallDevToolsProtocolMethodAsync("Security.enable", "{}");
            _securityEventReceiver = core.GetDevToolsProtocolEventReceiver("Security.visibleSecurityStateChanged");
            _securityStateHandler = OnVisibleSecurityStateChanged;
            _securityEventReceiver.DevToolsProtocolEventReceived += _securityStateHandler;
            _securityDevToolsEnabled = true;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WebView22Browser] Security.enable failed: {ex.Message}");
            ApplySchemeSecurityFallback();
        }
    }

    private void OnVisibleSecurityStateChanged(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        if (Tab == null)
            return;

        Tab.SecurityState = SecurityStateDevToolsParser.ParseVisibleSecurityStateChanged(e.ParameterObjectAsJson);
    }

    private void ApplySchemeSecurityFallback()
    {
        if (Tab == null)
            return;

        Tab.SecurityState = SecurityStateDevToolsParser.FromUriScheme(webView.Source?.ToString());
    }

    private async void OnPermissionRequested(object? sender, CoreWebView2PermissionRequestedEventArgs e)
    {
        if (PermissionStore != null &&
            PermissionStore.TryGet(e.Uri, e.PermissionKind, out var remembered))
        {
            e.State = remembered;
            return;
        }

        if (DialogService == null)
        {
            e.State = CoreWebView2PermissionState.Deny;
            return;
        }

        var allow = DialogService.PromptPermission(e.PermissionKind, e.Uri);
        e.State = allow ? CoreWebView2PermissionState.Allow : CoreWebView2PermissionState.Deny;

        if (PermissionStore != null)
            await PermissionStore.SetAsync(e.Uri, e.PermissionKind, e.State);
    }

    private void OnProcessFailed(object? sender, CoreWebView2ProcessFailedEventArgs e)
    {
        if (_isPermanentClose || Tab == null || _processRecoveryExhausted)
            return;

        _ = Dispatcher.InvokeAsync(HandleProcessFailedAsync);
    }

    private async Task HandleProcessFailedAsync()
    {
        if (_isPermanentClose || Tab == null || _processRecoveryExhausted)
            return;

        _processRecoveryAttempts++;

        UnwireEvents(resetPlaybackState: true);
        Tab.IsWebViewReady = false;
        _contentRevealed = false;

        if (_processRecoveryAttempts > MaxProcessRecoveryAttempts)
        {
            _processRecoveryExhausted = true;
            DialogService?.ShowError(
                "网页渲染进程多次崩溃，已停止自动恢复。请关闭此标签页后重新打开，或尝试清除浏览器用户数据后重启应用。",
                "进程错误");
            InitializationFailed?.Invoke(Tab);
            return;
        }

        DialogService?.ShowWarning("网页渲染进程已崩溃，正在尝试恢复...", "进程错误");

        if (Environment != null)
            await InitializeAsync();
    }

    private void UpdateHistoryState()
    {
        if (Tab == null)
            return;

        Tab.CanGoBack = webView.CanGoBack;
        Tab.CanGoForward = webView.CanGoForward;
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