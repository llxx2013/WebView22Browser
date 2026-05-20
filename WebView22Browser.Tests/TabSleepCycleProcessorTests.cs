using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Services;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

public class TabSleepCycleProcessorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private static BrowserTabViewModel CreateTab(
        Guid? tabId = null,
        bool isSelected = false,
        bool isWebViewReady = true,
        DateTime? lastActiveUtc = null)
    {
        var tab = new BrowserTabViewModel
        {
            TabId = tabId ?? Guid.NewGuid(),
            InitialUri = "https://example.com"
        };
        tab.IsWebViewReady = isWebViewReady;
        tab.IsSelected = isSelected;
        if (lastActiveUtc != null)
            tab.SetLastActiveUtc(lastActiveUtc.Value);
        return tab;
    }

    [Fact]
    public async Task ProcessAsync_WhenSleepDisabled_ReturnsFalse()
    {
        var processor = new TabSleepCycleProcessor();
        var tabHost = new TabHostService();
        var tab = CreateTab(lastActiveUtc: DateTime.UtcNow - Timeout * 2);
        tabHost.Register(tab.TabId, new FakeTabWebViewHost(tab));

        var anyAction = await processor.ProcessAsync(
            [tab],
            tabHost,
            new BrowserOptions { TabSleepTimeoutMinutes = 0 },
            SystemPressureLevel.Normal,
            DateTime.UtcNow);

        Assert.False(anyAction);
        Assert.Empty(((FakeTabWebViewHost)tabHost.GetHost(tab.TabId)!).Actions);
    }

    [Fact]
    public async Task ProcessAsync_IdleBackgroundTab_ReducesMemory()
    {
        var processor = new TabSleepCycleProcessor();
        var tabHost = new TabHostService();
        var now = DateTime.UtcNow;
        var tab = CreateTab(
            isSelected: false,
            lastActiveUtc: now - Timeout * 0.5);
        var fake = new FakeTabWebViewHost(tab);
        tabHost.Register(tab.TabId, fake);

        var anyAction = await processor.ProcessAsync(
            [tab],
            tabHost,
            new BrowserOptions { TabSleepTimeoutMinutes = (int)Timeout.TotalMinutes },
            SystemPressureLevel.Normal,
            now);

        Assert.True(anyAction);
        Assert.Contains(nameof(FakeTabWebViewHost.ReduceMemoryAsync), fake.Actions);
    }

    [Fact]
    public async Task ProcessAsync_IdleAtTimeout_SuspendsLight()
    {
        var processor = new TabSleepCycleProcessor();
        var tabHost = new TabHostService();
        var now = DateTime.UtcNow;
        var tab = CreateTab(
            isSelected: false,
            lastActiveUtc: now - Timeout);
        var fake = new FakeTabWebViewHost(tab);
        tabHost.Register(tab.TabId, fake);

        var anyAction = await processor.ProcessAsync(
            [tab],
            tabHost,
            new BrowserOptions { TabSleepTimeoutMinutes = (int)Timeout.TotalMinutes },
            SystemPressureLevel.Normal,
            now);

        Assert.True(anyAction);
        Assert.Contains(nameof(FakeTabWebViewHost.SuspendLightAsync), fake.Actions);
    }

    [Fact]
    public async Task ProcessAsync_SelectedTab_TakesNoAction()
    {
        var processor = new TabSleepCycleProcessor();
        var tabHost = new TabHostService();
        var now = DateTime.UtcNow;
        var tab = CreateTab(
            isSelected: true,
            lastActiveUtc: now - Timeout * 3);
        var fake = new FakeTabWebViewHost(tab);
        tabHost.Register(tab.TabId, fake);

        var anyAction = await processor.ProcessAsync(
            [tab],
            tabHost,
            new BrowserOptions { TabSleepTimeoutMinutes = (int)Timeout.TotalMinutes },
            SystemPressureLevel.Normal,
            now);

        Assert.False(anyAction);
        Assert.Empty(fake.Actions);
    }

    [Fact]
    public async Task ProcessAsync_HighPressure_DestroySleep()
    {
        var processor = new TabSleepCycleProcessor();
        var tabHost = new TabHostService();
        var now = DateTime.UtcNow;
        var tab = CreateTab(
            isSelected: false,
            lastActiveUtc: now - Timeout * 2);
        var fake = new FakeTabWebViewHost(tab);
        tabHost.Register(tab.TabId, fake);

        var anyAction = await processor.ProcessAsync(
            [tab],
            tabHost,
            new BrowserOptions { TabSleepTimeoutMinutes = (int)Timeout.TotalMinutes },
            SystemPressureLevel.High,
            now);

        Assert.True(anyAction);
        Assert.Contains(nameof(FakeTabWebViewHost.SuspendAsync), fake.Actions);
    }
}