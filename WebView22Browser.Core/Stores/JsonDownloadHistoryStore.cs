using System.Text.Json;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonDownloadHistoryStore : JsonFileStoreBase, IDownloadHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<DownloadHistoryEntry> _items = [];

    public JsonDownloadHistoryStore(BrowserOptions options)
        : this(options.GetDefaultDownloadHistoryPath())
    {
    }

    public JsonDownloadHistoryStore(string filePath)
        : base(filePath)
    {
    }

    public IReadOnlyList<DownloadHistoryEntry> Items => _items;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _items.Clear();

        if (!File.Exists(FilePath))
            return;

        try
        {
            await using var stream = File.OpenRead(FilePath);
            var loaded = await JsonSerializer.DeserializeAsync<List<DownloadHistoryEntry>>(stream, JsonOptions, cancellationToken);
            if (loaded != null)
                _items.AddRange(loaded);
        }
        catch (JsonException)
        {
            _items.Clear();
        }
    }

    public Task SaveAsync(CancellationToken cancellationToken = default) =>
        WriteAtomicAsync(_items, JsonOptions, cancellationToken);

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
        if (maxEntries <= 0 || _items.Count <= maxEntries)
            return;

        var completed = _items
            .Where(i => i.State != DownloadState.InProgress)
            .OrderByDescending(i => i.CompletedAtUtc ?? i.StartedAtUtc)
            .ToList();

        var inProgress = _items.Where(i => i.State == DownloadState.InProgress).ToList();
        var keepCompleted = completed.Take(Math.Max(0, maxEntries - inProgress.Count)).ToList();

        _items.Clear();
        _items.AddRange(inProgress);
        _items.AddRange(keepCompleted);
    }
}