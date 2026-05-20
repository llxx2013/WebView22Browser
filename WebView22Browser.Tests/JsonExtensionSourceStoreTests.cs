using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonExtensionSourceStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsItems()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ext-store-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonExtensionSourceStore(path);
            store.Add(@"C:\extensions\sample", "abc123");
            await store.SaveAsync();

            var reloaded = new JsonExtensionSourceStore(path);
            await reloaded.LoadAsync();

            Assert.Single(reloaded.Items);
            Assert.Equal("abc123", reloaded.Items[0].LastKnownExtensionId);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void Add_DuplicateFolderPath_UpdatesExtensionId()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ext-store-{Guid.NewGuid()}.json");
        var folder = Path.Combine(Path.GetTempPath(), "my-ext");
        var store = new JsonExtensionSourceStore(path);
        store.Add(folder, "old");
        store.Add(folder, "new");

        Assert.Single(store.Items);
        Assert.Equal("new", store.Items[0].LastKnownExtensionId);
    }

    [Fact]
    public void FindByFolderPath_EmptyPath_ReturnsNull()
    {
        var store = new JsonExtensionSourceStore(Path.Combine(Path.GetTempPath(), $"ext-store-{Guid.NewGuid()}.json"));
        store.Add(@"C:\extensions\sample", "id1");

        Assert.Null(store.FindByFolderPath(""));
        Assert.Null(store.FindByFolderPath("   "));
    }

    [Fact]
    public void RemoveByExtensionId_RemovesMatchingEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ext-store-{Guid.NewGuid()}.json");
        var store = new JsonExtensionSourceStore(path);
        store.Add(@"C:\a", "id1");
        store.Add(@"C:\b", "id2");

        store.RemoveByExtensionId("id1");

        Assert.Single(store.Items);
        Assert.Equal("id2", store.Items[0].LastKnownExtensionId);
    }
}
