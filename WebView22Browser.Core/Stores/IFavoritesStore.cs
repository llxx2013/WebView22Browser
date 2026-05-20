using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface IFavoritesStore
{
    IReadOnlyList<FavoriteItem> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    FavoriteItem AddOrUpdate(string title, string url);

    void Remove(Guid id);
}