using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeDownloadHistoryStore : IDownloadHistoryStore
{
    private readonly List<DownloadHistoryEntry> _items = [];

    public IReadOnlyList<DownloadHistoryEntry> Items => _items;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Upsert(DownloadHistoryEntry entry)
    {
        var index = _items.FindIndex(i => i.Id == entry.Id);
        if (index >= 0)
            _items[index] = entry;
        else
            _items.Insert(0, entry);
    }

    public void Remove(Guid id) => _items.RemoveAll(i => i.Id == id);

    public void ClearCompleted() =>
        _items.RemoveAll(i => i.State is DownloadState.Completed or DownloadState.Cancelled);

    public void TrimToMaxEntries(int maxEntries)
    {
        if (_items.Count > maxEntries)
            _items.RemoveRange(maxEntries, _items.Count - maxEntries);
    }
}
