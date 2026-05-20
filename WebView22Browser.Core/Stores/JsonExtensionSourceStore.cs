using System.Text.Json;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonExtensionSourceStore : IExtensionSourceStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly List<ExtensionSourceEntry> _items = [];

    public JsonExtensionSourceStore(BrowserOptions options)
        : this(options.GetDefaultExtensionsRegistryPath())
    {
    }

    public JsonExtensionSourceStore(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<ExtensionSourceEntry> Items => _items;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _items.Clear();

        if (!File.Exists(_filePath))
            return;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync<List<ExtensionSourceEntry>>(stream, JsonOptions, cancellationToken);
            if (loaded != null)
                _items.AddRange(loaded);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            _items.Clear();
        }
    }

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, _items, JsonOptions, cancellationToken);
    }

    public ExtensionSourceEntry Add(string folderPath, string? lastKnownExtensionId = null)
    {
        var normalized = Path.GetFullPath(folderPath.Trim());
        var existing = FindByFolderPath(normalized);
        if (existing != null)
        {
            if (lastKnownExtensionId != null)
                existing.LastKnownExtensionId = lastKnownExtensionId;
            return existing;
        }

        var entry = new ExtensionSourceEntry
        {
            FolderPath = normalized,
            LastKnownExtensionId = lastKnownExtensionId
        };
        _items.Add(entry);
        return entry;
    }

    public void Remove(Guid id) => _items.RemoveAll(i => i.Id == id);

    public void RemoveByExtensionId(string extensionId) =>
        _items.RemoveAll(i =>
            string.Equals(i.LastKnownExtensionId, extensionId, StringComparison.OrdinalIgnoreCase));

    public ExtensionSourceEntry? FindByFolderPath(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
            return null;

        var normalized = Path.GetFullPath(folderPath.Trim());
        return _items.FirstOrDefault(i =>
            string.Equals(Path.GetFullPath(i.FolderPath), normalized, StringComparison.OrdinalIgnoreCase));
    }

    public ExtensionSourceEntry? FindByExtensionId(string extensionId) =>
        _items.FirstOrDefault(i =>
            string.Equals(i.LastKnownExtensionId, extensionId, StringComparison.OrdinalIgnoreCase));
}