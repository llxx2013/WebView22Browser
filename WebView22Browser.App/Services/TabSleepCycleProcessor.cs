using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

/// <summary>
/// One timer-tick worth of tab sleep policy evaluation and host actions (extracted for unit tests).
/// </summary>
public sealed class TabSleepCycleProcessor
{
    public async Task<bool> ProcessAsync(
        IReadOnlyList<BrowserTabViewModel> tabs,
        ITabHostService tabHostService,
        BrowserOptions options,
        SystemPressureLevel pressure,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (options.TabSleepTimeoutMinutes <= 0)
            return false;

        var timeout = TimeSpan.FromMinutes(options.TabSleepTimeoutMinutes);
        var anyAction = false;

        foreach (var tab in tabs)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var idleDuration = utcNow - tab.LastActiveUtc;
            var candidate = new TabSleepCandidate(
                IsEnabled: true,
                Timeout: timeout,
                IsWebViewReady: tab.IsWebViewReady,
                IsSleeping: tab.IsSleeping,
                IsSelected: tab.IsSelected,
                IsLoading: tab.IsLoading,
                IsPlayingAudio: tab.IsPlayingAudio,
                HasActiveDownloads: tab.HasActiveDownloads,
                IdleDuration: idleDuration,
                Pressure: pressure,
                IsMemoryReduced: tab.IsMemoryReduced,
                IsLightSuspended: tab.IsLightSuspended);

            var action = TabSleepPolicy.Decide(candidate);
            if (action == TabSleepAction.None)
                continue;

            if (tabHostService.GetHost(tab.TabId) is not ITabWebViewHost host)
                continue;

            switch (action)
            {
                case TabSleepAction.ReduceMemory:
                    await host.ReduceMemoryAsync();
                    break;
                case TabSleepAction.Suspend:
                    await host.SuspendLightAsync();
                    break;
                case TabSleepAction.Destroy:
                    await host.SuspendAsync();
                    break;
            }

            anyAction = true;
        }

        return anyAction;
    }
}