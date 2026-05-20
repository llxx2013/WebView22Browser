using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonFavoritesStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsItems()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fav-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonFavoritesStore(path);
            store.AddOrUpdate("Bing", "https://www.bing.com");
            await store.SaveAsync();

            var reloaded = new JsonFavoritesStore(path);
            await reloaded.LoadAsync();

            Assert.Single(reloaded.Items);
            Assert.Equal("https://www.bing.com", reloaded.Items[0].Url);
            Assert.Equal("Bing", reloaded.Items[0].Title);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void AddOrUpdate_DuplicateUrl_UpdatesTitle()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fav-test-{Guid.NewGuid()}.json");
        var store = new JsonFavoritesStore(path);
        store.AddOrUpdate("Old", "https://example.com");
        store.AddOrUpdate("New", "https://example.com");

        Assert.Single(store.Items);
        Assert.Equal("New", store.Items[0].Title);
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"fav-test-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{ not valid json");

            var store = new JsonFavoritesStore(path);
            await store.LoadAsync();

            Assert.Empty(store.Items);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Remove_DeletesItem()
    {
        var store = new JsonFavoritesStore(Path.Combine(Path.GetTempPath(), $"fav-test-{Guid.NewGuid()}.json"));
        var item = store.AddOrUpdate("Test", "https://test.com");
        store.Remove(item.Id);
        Assert.Empty(store.Items);
    }
}