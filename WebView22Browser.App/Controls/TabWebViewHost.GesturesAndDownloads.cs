using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.WebView2;

namespace WebView22Browser.App.Controls;

/// <summary>
/// New-tab gestures, context menu overrides, downloads, and media playback signals.
/// </summary>
public partial class TabWebViewHost
{
    private bool _middleClickOpensNewTab;
    private CancellationTokenSource? _middleClickResetCts;

    private void OnIsDocumentPlayingAudioChanged(object? sender, object e)
    {
        if (_isPermanentClose || Tab == null || webView.CoreWebView2 == null)
            return;

        Tab.IsPlayingAudio = webView.CoreWebView2.IsDocumentPlayingAudio;
        if (Tab.IsPlayingAudio)
            Tab.TouchActivity();
    }

    private void OnNewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        if (_isPermanentClose || Tab == null)
            return;

        e.Handled = true;
        if (TabHostCallbacks == null || Tab == null)
            return;

        var openInNewTab = e.IsUserInitiated
            || NewTabGestureDetector.IsControlClick()
            || ConsumeMiddleClickNewTab();
        TabHostCallbacks.HandleNavigation(e.Uri, Tab, openInNewTab);
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
            TabHostCallbacks?.HandleNavigation(linkUri, sourceTab, openInNewTab: true);

        e.MenuItems.Insert(0, openInNewTabItem);
    }

    private void OnDownloadStarting(object? sender, CoreWebView2DownloadStartingEventArgs e)
    {
        if (DownloadService == null || Tab == null)
            return;

        DownloadService.HandleDownloadStarting(e, Tab);
    }
}