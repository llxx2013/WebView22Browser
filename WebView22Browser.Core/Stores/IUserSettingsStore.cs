using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface IUserSettingsStore
{
    BrowserSettingsDto? Current { get; }

    string FilePath { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(BrowserSettingsDto settings, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
