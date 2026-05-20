using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeFavoritesStore : IFavoritesStore
{
    private readonly List<FavoriteItem> _items = [];

    public IReadOnlyList<FavoriteItem> Items => _items;

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public FavoriteItem AddOrUpdate(string title, string url)
    {
        var existing = _items.FirstOrDefault(i =>
            string.Equals(i.Url, url, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            existing.Title = title;
            return existing;
        }

        var item = new FavoriteItem { Title = title, Url = url };
        _items.Add(item);
        return item;
    }

    public void Remove(Guid id) => _items.RemoveAll(i => i.Id == id);
}
