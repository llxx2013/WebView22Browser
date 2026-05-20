using System.Text.Json;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonTabSessionStore : ITabSessionStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private static readonly TimeSpan DebounceInterval = TimeSpan.FromSeconds(2);

    private readonly string _filePath;
    private readonly object _lock = new();
    private TabSessionFile? _pending;
    private DateTime _lastFlushUtc = DateTime.MinValue;
    private bool _dirty;

    public JsonTabSessionStore(BrowserOptions options)
        : this(options.GetDefaultTabSessionPath())
    {
    }

    public JsonTabSessionStore(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<TabSessionFile?> LoadAsync(CancellationToken cancellationToken = default)
    {
        string path;
        lock (_lock)
        {
            path = _filePath;
        }

        if (!File.Exists(path))
            return null;

        try
        {
            await using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);
            var loaded = await JsonSerializer.DeserializeAsync<TabSessionFile>(stream, JsonOptions, cancellationToken);
            if (loaded is { Version: 1, Tabs.Count: > 0 })
                return loaded;
        }
        catch (JsonException)
        {
            // Corrupt file — treat as no session.
        }

        return null;
    }

    public void Update(TabSessionFile snapshot)
    {
        lock (_lock)
        {
            _pending = snapshot;
            _dirty = true;
        }
    }

    public void MarkDirty()
    {
        lock (_lock)
        {
            _dirty = true;
        }
    }

    public Task FlushAsync(CancellationToken cancellationToken = default) =>
        FlushCoreAsync(force: true, cancellationToken);

    public Task FlushIfDebouncedAsync(CancellationToken cancellationToken = default) =>
        FlushCoreAsync(force: false, cancellationToken);

    private async Task FlushCoreAsync(bool force, CancellationToken cancellationToken)
    {
        TabSessionFile? toWrite = null;
        var shouldWrite = false;

        lock (_lock)
        {
            if (!_dirty || _pending == null)
                return;

            if (!force && DateTime.UtcNow - _lastFlushUtc < DebounceInterval)
                return;

            toWrite = _pending;
            _dirty = false;
            _lastFlushUtc = DateTime.UtcNow;
            shouldWrite = true;
        }

        if (shouldWrite && toWrite != null)
            await WriteAtomicAsync(toWrite, cancellationToken);
    }

    private async Task WriteAtomicAsync(TabSessionFile snapshot, CancellationToken cancellationToken)
    {
        string path;
        lock (_lock)
        {
            path = _filePath;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, snapshot, JsonOptions, cancellationToken);

        File.Move(tempPath, path, overwrite: true);
    }
}
