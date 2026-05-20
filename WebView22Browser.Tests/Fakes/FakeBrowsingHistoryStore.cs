using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeBrowsingHistoryStore : IBrowsingHistoryStore
{
    private readonly List<BrowsingHistoryEntry> _items = [];

    public IReadOnlyList<BrowsingHistoryEntry> Items => _items;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public void Add(BrowsingHistoryEntry entry) => _items.Insert(0, entry);

    public void AddAndTrimToMax(BrowsingHistoryEntry entry, int maxEntries)
    {
        _items.Insert(0, entry);
        TrimToMaxEntries(maxEntries);
    }

    public void Remove(Guid id) => _items.RemoveAll(i => i.Id == id);

    public void Clear() => _items.Clear();

    public void TrimToMaxEntries(int maxEntries)
    {
        if (_items.Count > maxEntries)
            _items.RemoveRange(maxEntries, _items.Count - maxEntries);
    }
}
