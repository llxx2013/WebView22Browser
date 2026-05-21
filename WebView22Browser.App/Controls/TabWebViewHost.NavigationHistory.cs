using System.Diagnostics;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.ViewModels;
using WebView22Browser.App.WebView2;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Controls;

/// <summary>
/// Per-tab navigation history, frozen session restore, and navigation event handlers.
/// </summary>
public partial class TabWebViewHost
{
    private TabNavigationHistory? _navigationHistory;
    private bool _isRestoringHistory;
    private TaskCompletionSource<bool>? _navigationWaiter;

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

    private TabNavigationHistory NavigationHistory =>
        _navigationHistory ??= CreateNavigationHistory();

    private TabNavigationHistory CreateNavigationHistory()
    {
        var maxEntries = BrowserOptions?.TabHistoryMaxEntries ?? 50;
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

    private void UpdateHistoryState()
    {
        if (Tab == null)
            return;

        Tab.CanGoBack = webView.CanGoBack;
        Tab.CanGoForward = webView.CanGoForward;
    }

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (_isPermanentClose || Tab == null)
            return;

        if (webView.CoreWebView2 is { } core)
        {
            UserScriptBridge?.ClearMenuCommands(core);
        }

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
            if (webView.CoreWebView2 is { } probeCore)
                _ = ReinjectUserScriptsIfNeededAsync(probeCore);
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

        if (Tab.IsSelected)
            TabHostCallbacks?.UpdateWindowTitle();
    }

    private void OnContentLoading(object? sender, CoreWebView2ContentLoadingEventArgs e)
    {
        if (_isPermanentClose || Tab == null || e.IsErrorPage)
            return;

        Tab.LoadingStatus = "正在加载页面内容...";
    }
}