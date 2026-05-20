using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonUserSettingsStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsSettings()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-settings-{Guid.NewGuid():N}.json");
        var store = new JsonUserSettingsStore(path);
        var dto = new BrowserSettingsDto
        {
            HomeUrl = "https://custom.example",
            TabSleepTimeoutMinutes = 0,
            BrowsingHistoryMaxEntries = 500
        };

        await store.SaveAsync(dto);
        await store.LoadAsync();

        Assert.NotNull(store.Current);
        Assert.Equal("https://custom.example", store.Current!.HomeUrl);
        Assert.Equal(0, store.Current.TabSleepTimeoutMinutes);
        Assert.Equal(500, store.Current.BrowsingHistoryMaxEntries);
    }

    [Fact]
    public async Task ClearAsync_RemovesFileAndCurrent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-settings-{Guid.NewGuid():N}.json");
        var store = new JsonUserSettingsStore(path);
        await store.SaveAsync(new BrowserSettingsDto { HomeUrl = "https://x.com" });
        await store.ClearAsync();

        Assert.Null(store.Current);
        Assert.False(File.Exists(path));
    }
}