using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class DownloadsViewModel : ObservableObject
{
    private readonly IDownloadHistoryStore _historyStore;
    private readonly BrowserOptions _options;

    public DownloadsViewModel(IDownloadHistoryStore historyStore, BrowserOptions options)
    {
        _historyStore = historyStore;
        _options = options;
        ActiveItems = new ObservableCollection<DownloadItemViewModel>();
        HistoryItems = new ObservableCollection<DownloadItemViewModel>();
        ActiveItems.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(HasActiveDownloads));
        };
    }

    public ObservableCollection<DownloadItemViewModel> ActiveItems { get; }

    public ObservableCollection<DownloadItemViewModel> HistoryItems { get; }

    public int ActiveCount => ActiveItems.Count;

    public bool HasActiveDownloads => ActiveCount > 0;

    [ObservableProperty]
    private bool _isPanelOpen;

    public async Task LoadHistoryAsync()
    {
        await _historyStore.LoadAsync();
        HistoryItems.Clear();

        foreach (var entry in _historyStore.Items)
        {
            if (entry.State == DownloadState.InProgress)
            {
                entry.State = DownloadState.Interrupted;
                entry.CompletedAtUtc ??= DateTime.UtcNow;
            }

            var item = DownloadItemViewModel.FromHistoryEntry(entry);
            WireHistoryItem(item);
            HistoryItems.Add(item);
        }
    }

    public void AddActiveItem(DownloadItemViewModel item)
    {
        WireHistoryItem(item);
        ActiveItems.Insert(0, item);
        IsPanelOpen = true;
    }

    public async Task MoveToHistoryAsync(DownloadItemViewModel item)
    {
        if (ActiveItems.Remove(item))
        {
            OnPropertyChanged(nameof(ActiveCount));
            OnPropertyChanged(nameof(HasActiveDownloads));
        }

        item.ClearOperations();
        if (!HistoryItems.Contains(item))
        {
            WireHistoryItem(item);
            HistoryItems.Insert(0, item);
        }

        _historyStore.Upsert(item.ToHistoryEntry());
        _historyStore.TrimToMaxEntries(_options.DownloadHistoryMaxEntries);
        await _historyStore.SaveAsync();
    }

    public async Task RemoveFromHistoryAsync(DownloadItemViewModel item)
    {
        HistoryItems.Remove(item);
        _historyStore.Remove(item.Id);
        await _historyStore.SaveAsync();
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        var toRemove = HistoryItems
            .Where(i => i.State is DownloadState.Completed or DownloadState.Cancelled)
            .ToList();

        foreach (var item in toRemove)
            HistoryItems.Remove(item);

        _historyStore.ClearCompleted();
        await _historyStore.SaveAsync();
    }

    private void WireHistoryItem(DownloadItemViewModel item)
    {
        item.ShowInFolderRequested += OnShowInFolderRequested;
        item.OpenRequested += OnOpenRequested;
        item.RemoveFromHistoryRequested += OnRemoveFromHistoryRequested;
    }

    private void UnwireHistoryItem(DownloadItemViewModel item)
    {
        item.ShowInFolderRequested -= OnShowInFolderRequested;
        item.OpenRequested -= OnOpenRequested;
        item.RemoveFromHistoryRequested -= OnRemoveFromHistoryRequested;
    }

    public Action<string>? ShowInFolderHandler { get; set; }

    public Action<string>? OpenFileHandler { get; set; }

    private void OnShowInFolderRequested(string path) => ShowInFolderHandler?.Invoke(path);

    private void OnOpenRequested(string path) => OpenFileHandler?.Invoke(path);

    private async void OnRemoveFromHistoryRequested(DownloadItemViewModel item)
    {
        UnwireHistoryItem(item);
        await RemoveFromHistoryAsync(item);
    }
}
