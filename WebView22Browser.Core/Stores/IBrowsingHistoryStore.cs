using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface IBrowsingHistoryStore
{
    IReadOnlyList<BrowsingHistoryEntry> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    void Add(BrowsingHistoryEntry entry);

    void AddAndTrimToMax(BrowsingHistoryEntry entry, int maxEntries);

    void Remove(Guid id);

    void Clear();

    void TrimToMaxEntries(int maxEntries);
}
