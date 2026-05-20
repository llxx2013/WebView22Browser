using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Threading;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class TabSleepService : IDisposable
{
    private readonly MainViewModel _mainViewModel;
    private readonly ITabHostService _tabHostService;
    private readonly ISystemPressureMonitor _pressureMonitor;
    private readonly ITabSessionStore _sessionStore;
    private readonly BrowserOptions _options;
    private DispatcherTimer? _timer;
    private bool _isStarted;
    private bool _suppressNextWake;
    private bool _sessionSnapshotStale = true;
    private BrowserTabViewModel? _previousSelectedTab;

    public TabSleepService(
        MainViewModel mainViewModel,
        ITabHostService tabHostService,
        ISystemPressureMonitor pressureMonitor,
        ITabSessionStore sessionStore,
        BrowserOptions options)
    {
        _mainViewModel = mainViewModel;
        _tabHostService = tabHostService;
        _pressureMonitor = pressureMonitor;
        _sessionStore = sessionStore;
        _options = options;
        _mainViewModel.PropertyChanged += OnMainViewModelPropertyChanged;
    }

    public void SuppressNextWake() => _suppressNextWake = true;

    public void Start(Dispatcher dispatcher)
    {
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
        }
    }

    public void Dispose()
    {
        Stop();
        _mainViewModel.PropertyChanged -= OnMainViewModelPropertyChanged;
    }

    private void OnMainViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainViewModel.SelectedTab))
            return;

        try
        {
            var selected = _mainViewModel.SelectedTab;
            if (_previousSelectedTab != null && _previousSelectedTab != selected)
                _previousSelectedTab.TouchActivity();

            if (selected == null)
            {
                _previousSelectedTab = null;
                return;
            }

            selected.TouchActivity();

            if (_suppressNextWake)
                _suppressNextWake = false;
            else
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
            if (_options.TabSleepTimeoutMinutes <= 0)
                return;

            var timeout = TimeSpan.FromMinutes(_options.TabSleepTimeoutMinutes);
            var now = DateTime.UtcNow;
            var pressure = _pressureMonitor.Current;
            var anyAction = false;

            foreach (var tab in _mainViewModel.Tabs.ToList())
            {
                var idleDuration = now - tab.LastActiveUtc;
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

                if (_tabHostService.GetHost(tab.TabId) is not { } host)
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
                MarkSessionDirty();
            }

            if (anyAction)
                PersistSessionSnapshotIfStale();

            await _sessionStore.FlushIfDebouncedAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"TabSleep tick failed: {ex}");
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
        foreach (var tab in _mainViewModel.Tabs)
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
            _mainViewModel.SelectedTab?.TabId,
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
