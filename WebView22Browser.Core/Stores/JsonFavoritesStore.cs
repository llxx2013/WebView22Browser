using System.Text.Json;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public sealed class JsonFavoritesStore : IFavoritesStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _filePath;
    private readonly List<FavoriteItem> _items = [];

    public JsonFavoritesStore(BrowserOptions options)
        : this(options.GetDefaultFavoritesPath())
    {
    }

    public JsonFavoritesStore(string filePath)
    {
        _filePath = filePath;
    }

    public IReadOnlyList<FavoriteItem> Items => _items;

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _items.Clear();

        if (!File.Exists(_filePath))
            return;

        try
        {
            await using var stream = File.OpenRead(_filePath);
            var loaded = await JsonSerializer.DeserializeAsync<List<FavoriteItem>>(stream, JsonOptions, cancellationToken);
            if (loaded != null)
                _items.AddRange(loaded);
        }
        catch (JsonException)
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

    public FavoriteItem AddOrUpdate(string title, string url)
    {
        var normalizedUrl = url.Trim();
        var existing = _items.FirstOrDefault(i =>
            string.Equals(i.Url, normalizedUrl, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.Title = string.IsNullOrWhiteSpace(title) ? normalizedUrl : title.Trim();
            return existing;
        }

        var item = new FavoriteItem
        {
            Title = string.IsNullOrWhiteSpace(title) ? normalizedUrl : title.Trim(),
            Url = normalizedUrl
        };
        _items.Add(item);
        return item;
    }

    public void Remove(Guid id) => _items.RemoveAll(i => i.Id == id);
}