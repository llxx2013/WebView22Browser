using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Async;

namespace WebView22Browser.App.Controls;

/// <summary>
/// WebView2 lifecycle: init, shutdown, suspend/wake, event wiring, and process recovery.
/// </summary>
public partial class TabWebViewHost
{
    private const int MaxProcessRecoveryAttempts = 3;

    private bool _isPermanentClose;
    private int _processRecoveryAttempts;
    private bool _processRecoveryExhausted;
    private bool _hasShutdown;
    private bool _isWired;
    private CancellationTokenSource? _initCts;
    private bool _contentRevealed;
    private bool _isHostConfigured;

    private void UserControl_Loaded(object sender, RoutedEventArgs e) =>
        FireAndForget.Run(TryBeginHostLifecycleAsync, ReportHostLifecycleFailure);

    private async Task TryBeginHostLifecycleAsync()
    {
        if (!_isHostConfigured || Tab == null)
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

    private void ReportHostLifecycleFailure(Exception ex) =>
        Trace.WriteLine($"[WebView22Browser] Host lifecycle failed: {ex.Message}");

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
        if (!_isHostConfigured || _hasShutdown || _isPermanentClose || Tab == null)
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

    private async Task InitializeAsync()
    {
        if (!_isHostConfigured || Tab == null || Environment == null || _isPermanentClose)
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

            TabHostCallbacks?.NotifyTabReadyChanged();

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

        UnwireSecurityMonitoring(core);

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

    private async Task ReinjectUserScriptsIfNeededAsync(CoreWebView2 core)
    {
        try
        {
            var needsReinject = await core.ExecuteScriptAsync(
                "(() => typeof $ === 'undefined' || typeof Swal === 'undefined' || typeof hotkeys === 'undefined')()");

            if (needsReinject.Trim().Equals("true", StringComparison.OrdinalIgnoreCase)
                && UserScriptService != null)
            {
                await UserScriptService.ReinjectPageEnvironmentAsync(core);
            }

            if (Tab != null)
                TabHostCallbacks?.RefreshScriptCommands(Tab);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WebView22Browser] User script reinject failed: {ex.Message}");
        }
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
}