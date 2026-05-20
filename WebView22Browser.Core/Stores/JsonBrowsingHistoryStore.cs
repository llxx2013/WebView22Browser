using System.Text.Json;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonBrowsingHistoryStore : JsonFileStoreBase, IBrowsingHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly List<BrowsingHistoryEntry> _items = [];
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonBrowsingHistoryStore(BrowserOptions options)
        : this(options.GetDefaultBrowsingHistoryPath())
    {
    }

    public JsonBrowsingHistoryStore(string filePath)
        : base(filePath, serializeWrites: true)
    {
    }

    public IReadOnlyList<BrowsingHistoryEntry> Items
    {
        get
        {
            _lock.Wait();
            try
            {
                return _items.ToList();
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            _items.Clear();

            if (!File.Exists(FilePath))
                return;

            try
            {
                await using var stream = File.OpenRead(FilePath);
                var loaded = await JsonSerializer.DeserializeAsync<List<BrowsingHistoryEntry>>(
                    stream, JsonOptions, cancellationToken);
                if (loaded != null)
                    _items.AddRange(loaded);
            }
            catch (JsonException)
            {
                _items.Clear();
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        List<BrowsingHistoryEntry> snapshot;
        await _lock.WaitAsync(cancellationToken);
        try
        {
            snapshot = _items.ToList();
        }
        finally
        {
            _lock.Release();
        }

        await WriteAtomicAsync(snapshot, JsonOptions, cancellationToken);
    }

    public void Add(BrowsingHistoryEntry entry)
    {
        _lock.Wait();
        try
        {
            _items.Insert(0, entry);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void AddAndTrimToMax(BrowsingHistoryEntry entry, int maxEntries)
    {
        _lock.Wait();
        try
        {
            _items.Insert(0, entry);
            TrimToMaxEntriesCore(maxEntries);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Remove(Guid id)
    {
        _lock.Wait();
        try
        {
            _items.RemoveAll(i => i.Id == id);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Clear()
    {
        _lock.Wait();
        try
        {
            _items.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void TrimToMaxEntries(int maxEntries)
    {
        _lock.Wait();
        try
        {
            TrimToMaxEntriesCore(maxEntries);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void TrimToMaxEntriesCore(int maxEntries)
    {
        if (maxEntries <= 0 || _items.Count <= maxEntries)
            return;

        _items.RemoveRange(maxEntries, _items.Count - maxEntries);
    }
}