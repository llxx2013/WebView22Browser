using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebView22Browser.App.Services;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly IBrowsingHistoryStore _store;
    private readonly IBrowsingHistoryService _historyService;
    private readonly int _searchDebounceMs;
    private CancellationTokenSource? _searchCts;

    public HistoryViewModel(
        IBrowsingHistoryStore store,
        IBrowsingHistoryService historyService,
        TimeSpan? searchDebounce = null)
    {
        _store = store;
        _historyService = historyService;
        _searchDebounceMs = searchDebounce.HasValue
            ? (int)searchDebounce.Value.TotalMilliseconds
            : 150;
        GroupedEntries = new ObservableCollection<HistoryGroupViewModel>();
        _historyService.HistoryChanged += OnHistoryChanged;
    }

    public ObservableCollection<HistoryGroupViewModel> GroupedEntries { get; }

    public Action<string>? NavigateToUrl { get; set; }

    [ObservableProperty]
    private bool _isPageOpen;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _hasNoResults;

    public async Task LoadAsync()
    {
        await _store.LoadAsync();
        RefreshGroupedEntries();
    }

    partial void OnSearchQueryChanged(string value) => ScheduleSearchRefresh();

    partial void OnIsPageOpenChanged(bool value)
    {
        if (value)
            RefreshGroupedEntries();
    }

    [RelayCommand]
    private void TogglePage() => IsPageOpen = !IsPageOpen;

    [RelayCommand]
    private void ClosePage() => IsPageOpen = false;

    [RelayCommand]
    private void OpenEntry(HistoryEntryViewModel? entry)
    {
        if (entry == null)
            return;

        NavigateToUrl?.Invoke(entry.Url);
        IsPageOpen = false;
    }

    [RelayCommand]
    private async Task RemoveEntryAsync(HistoryEntryViewModel? entry)
    {
        if (entry == null)
            return;

        _store.Remove(entry.Id);
        await _store.SaveAsync();
        RefreshGroupedEntries();
    }

    [RelayCommand]
    private async Task ClearAllAsync()
    {
        _store.Clear();
        await _store.SaveAsync();
        RefreshGroupedEntries();
    }

    private void OnHistoryChanged()
    {
        if (IsPageOpen)
            RefreshGroupedEntries();
    }

    private void ScheduleSearchRefresh()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (_searchDebounceMs <= 0)
        {
            RefreshGroupedEntries();
            return;
        }

        _ = DebouncedSearchRefreshAsync(token);
    }

    private async Task DebouncedSearchRefreshAsync(CancellationToken token)
    {
        try
        {
            await Task.Delay(_searchDebounceMs, token);
            if (token.IsCancellationRequested)
                return;

            var dispatcher = Application.Current?.Dispatcher;
            if (dispatcher != null && !dispatcher.CheckAccess())
                await dispatcher.InvokeAsync(RefreshGroupedEntries, DispatcherPriority.Background);
            else
                RefreshGroupedEntries();
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer keystroke.
        }
    }

    private void RefreshGroupedEntries()
    {
        var filtered = BrowsingHistorySearch.Filter(_store.Items, SearchQuery).ToList();
        var groups = BrowsingHistoryGrouper.GroupByDate(filtered);

        GroupedEntries.Clear();
        foreach (var group in groups)
        {
            var items = group.Entries.Select(e => new HistoryEntryViewModel(e));
            GroupedEntries.Add(new HistoryGroupViewModel(group.Label, items));
        }

        HasNoResults = GroupedEntries.Count == 0;
    }
}
