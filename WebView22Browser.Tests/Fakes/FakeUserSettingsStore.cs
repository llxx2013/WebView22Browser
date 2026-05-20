using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeUserSettingsStore : IUserSettingsStore
{
    public BrowserSettingsDto? Current { get; private set; }

    public string FilePath { get; } = Path.Combine(Path.GetTempPath(), "fake-user-settings.json");

    public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SaveAsync(BrowserSettingsDto settings, CancellationToken cancellationToken = default)
    {
        Current = settings;
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Current = null;
        return Task.CompletedTask;
    }
}