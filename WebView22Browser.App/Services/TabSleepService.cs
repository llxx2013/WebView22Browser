using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;

using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class TabSleepService : IRuntimeBrowserSettingsApplier, IDisposable
{
    private readonly TabSleepCycleProcessor _cycleProcessor = new();
    private readonly Lazy<MainViewModel> _mainViewModel;
    private readonly ITabHostService _tabHostService;
    private readonly ISystemPressureMonitor _pressureMonitor;
    private readonly ITabSessionStore _sessionStore;
    private readonly IBrowserStatusReporter _statusReporter;
    private readonly BrowserOptions _options;
    private Dispatcher? _dispatcher;
    private DispatcherTimer? _timer;
    private bool _isStarted;
    private bool _mainViewModelSubscribed;
    private bool _sessionSnapshotStale = true;
    private BrowserTabViewModel? _previousSelectedTab;

    public TabSleepService(
        Lazy<MainViewModel> mainViewModel,
        ITabHostService tabHostService,
        ISystemPressureMonitor pressureMonitor,
        ITabSessionStore sessionStore,
        IBrowserStatusReporter statusReporter,
        BrowserOptions options)
    {
        _mainViewModel = mainViewModel;
        _tabHostService = tabHostService;
        _pressureMonitor = pressureMonitor;
        _sessionStore = sessionStore;
        _statusReporter = statusReporter;
        _options = options;
    }

    public void Start(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
        EnsureMainViewModelSubscription();
        if (_isStarted || _options.TabSleepTimeoutMinutes <= 0)
            return;

        _isStarted = true;
        _pressureMonitor.Start();
        var intervalSeconds = Math.Max(1, _options.TabSleepCheckIntervalSeconds);
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(intervalSeconds)
        };
        _timer.Tick += OnTimerTick;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Tick -= OnTimerTick;
            _timer.Stop();
            _timer = null;
        }

        _pressureMonitor.Stop();
        _isStarted = false;
    }

    public void ApplyRuntimeSettings()
    {
        if (_dispatcher == null)
            return;

        if (_options.TabSleepTimeoutMinutes <= 0)
        {
            Stop();
            return;
        }

        if (!_isStarted)
        {
            Start(_dispatcher);
            return;
        }

        var intervalSeconds = Math.Max(1, _options.TabSleepCheckIntervalSeconds);
        if (_timer != null)
            _timer.Interval = TimeSpan.FromSeconds(intervalSeconds);
    }

    public void MarkSessionDirty()
    {
        _sessionSnapshotStale = true;
        _sessionStore.MarkDirty();
    }

    public async Task FlushAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var snapshot = BuildSessionSnapshot();
            if (snapshot.Tabs.Count > 0)
                _sessionStore.Update(snapshot);

            _sessionSnapshotStale = false;
            await _sessionStore.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TabSleep flush failed: {ex}");
            _statusReporter.Report("标签会话保存失败，部分状态可能未写入磁盘。");
        }
    }

    public void Dispose()
    {
        Stop();
        if (_mainViewModelSubscribed)
            _mainViewModel.Value.PropertyChanged -= OnMainViewModelPropertyChanged;
    }

    private void EnsureMainViewModelSubscription()
    {
        if (_mainViewModelSubscribed)
            return;

        _mainViewModel.Value.PropertyChanged += OnMainViewModelPropertyChanged;
        _mainViewModelSubscribed = true;
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.SelectedTab))
            return;

        try
        {
            var selected = _mainViewModel.Value.SelectedTab;
            if (_previousSelectedTab != null && _previousSelectedTab != selected)
                _previousSelectedTab.TouchActivity();

            if (selected == null)
            {
                _previousSelectedTab = null;
                return;
            }

            selected.TouchActivity();

            _ = WakeTabIfNeededAsync(selected);

            MarkSessionDirty();
            _previousSelectedTab = selected;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TabSleep selection handler failed: {ex}");
        }
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        try
        {
            var anyAction = await _cycleProcessor.ProcessAsync(
                _mainViewModel.Value.Tabs.ToList(),
                _tabHostService,
                _options,
                _pressureMonitor.Current,
                DateTime.UtcNow);

            if (anyAction)
            {
                MarkSessionDirty();
                PersistSessionSnapshotIfStale();
            }

            await _sessionStore.FlushIfDebouncedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TabSleep tick failed: {ex}");
            _statusReporter.Report("标签休眠检查失败，请查看调试输出。");
        }
    }

    private void PersistSessionSnapshotIfStale()
    {
        if (!_sessionSnapshotStale)
            return;

        var snapshot = BuildSessionSnapshot();
        if (snapshot.Tabs.Count > 0)
        {
            _sessionStore.Update(snapshot);
            _sessionSnapshotStale = false;
        }
    }

    private TabSessionFile BuildSessionSnapshot()
    {
        var sources = new List<TabSessionSource>();
        foreach (var tab in _mainViewModel.Value.Tabs)
        {
            IReadOnlyList<string>? history = tab.FrozenHistoryEntries;
            var index = tab.FrozenHistoryIndex;

            if (_tabHostService.GetHost(tab.TabId) is { } host)
            {
                var snapshot = host.TryGetHistorySnapshot();
                if (snapshot != null)
                {
                    history = snapshot.Value.Entries;
                    index = snapshot.Value.CurrentIndex;
                }
            }

            sources.Add(new TabSessionSource(
                tab.TabId,
                tab.Title,
                tab.Address,
                history,
                index,
                tab.LastActiveUtc));
        }

        return TabSessionSnapshotBuilder.Build(
            sources,
            _mainViewModel.Value.SelectedTab?.TabId,
            _options.TabHistoryMaxEntries);
    }

    private async Task WakeTabIfNeededAsync(BrowserTabViewModel tab)
    {
        if (!tab.IsSleeping && !tab.IsLightSuspended)
            return;

        if (_tabHostService.GetHost(tab.TabId) is { } host)
            await host.WakeAsync();
    }
}