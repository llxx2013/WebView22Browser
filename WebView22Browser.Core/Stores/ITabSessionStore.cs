using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface ITabSessionStore
{
    Task<TabSessionFile?> LoadAsync(CancellationToken cancellationToken = default);

    void Update(TabSessionFile snapshot);

    void MarkDirty();

    Task FlushAsync(CancellationToken cancellationToken = default);

    /// <summary>Writes pending snapshot only if dirty and debounce interval has elapsed.</summary>
    Task FlushIfDebouncedAsync(CancellationToken cancellationToken = default);
}
