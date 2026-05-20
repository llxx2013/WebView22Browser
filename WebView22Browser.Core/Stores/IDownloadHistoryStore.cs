using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface IDownloadHistoryStore
{
    IReadOnlyList<DownloadHistoryEntry> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    void Upsert(DownloadHistoryEntry entry);

    void Remove(Guid id);

    void ClearCompleted();

    void TrimToMaxEntries(int maxEntries);
}
