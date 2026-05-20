using System.Text.Json;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

/// <summary>
/// JSON-backed user script store. Thread-safe for concurrent reads/writes from the UI thread and async save/load.
/// </summary>
public sealed class JsonUserScriptStore : JsonFileStoreBase, IUserScriptStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _sync = new();
    private readonly List<UserScriptEntry> _items = [];

    public JsonUserScriptStore(BrowserOptions options)
        : this(options.GetDefaultUserScriptsPath())
    {
    }

    public JsonUserScriptStore(string filePath)
        : base(filePath)
    {
    }

    public IReadOnlyList<UserScriptEntry> Items
    {
        get
        {
            lock (_sync)
                return _items.ToList();
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        List<UserScriptEntry>? loaded = null;

        if (File.Exists(FilePath))
        {
            try
            {
                await using var stream = File.OpenRead(FilePath);
                loaded = await JsonSerializer.DeserializeAsync<List<UserScriptEntry>>(stream, JsonOptions, cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                loaded = null;
            }
        }

        lock (_sync)
        {
            _items.Clear();
            if (loaded != null)
                _items.AddRange(loaded);
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        List<UserScriptEntry> snapshot;
        lock (_sync)
            snapshot = _items.ToList();

        await WriteAtomicAsync(snapshot, JsonOptions, cancellationToken);
    }

    public UserScriptEntry Add(UserScriptEntry entry)
    {
        lock (_sync)
        {
            if (entry.Id == Guid.Empty)
                entry.Id = Guid.NewGuid();

            entry.CreatedAtUtc = entry.CreatedAtUtc == default ? DateTime.UtcNow : entry.CreatedAtUtc;
            entry.UpdatedAtUtc = DateTime.UtcNow;
            entry.MatchPatterns = entry.MatchPatterns.ToArray();
            entry.ExcludePatterns = entry.ExcludePatterns.ToArray();
            entry.ConnectPatterns = entry.ConnectPatterns.ToArray();
            entry.RequireUrls = entry.RequireUrls.ToArray();
            entry.Resources = new Dictionary<string, string>(entry.Resources, StringComparer.Ordinal);
            _items.Add(entry);
            return entry;
        }
    }

    public void Update(UserScriptEntry entry)
    {
        lock (_sync)
        {
            var existing = _items.FirstOrDefault(i => i.Id == entry.Id);
            if (existing == null)
                return;

            existing.CopyFrom(entry);
        }
    }

    public void Remove(Guid id)
    {
        lock (_sync)
            _items.RemoveAll(i => i.Id == id);
    }

    public UserScriptEntry? FindById(Guid id)
    {
        lock (_sync)
            return _items.FirstOrDefault(i => i.Id == id);
    }
}