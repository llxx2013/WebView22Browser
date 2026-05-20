using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonUserScriptStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsItems()
    {
        var path = Path.Combine(Path.GetTempPath(), $"userscript-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonUserScriptStore(path);
            store.Add(new UserScriptEntry
            {
                Name = "Test",
                MatchPatterns = ["*://*.example.com/*"],
                Code = "console.log(1);"
            });
            await store.SaveAsync();

            var reloaded = new JsonUserScriptStore(path);
            await reloaded.LoadAsync();

            Assert.Single(reloaded.Items);
            Assert.Equal("Test", reloaded.Items[0].Name);
            Assert.Equal("console.log(1);", reloaded.Items[0].Code);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_CorruptJson_ReturnsEmpty()
    {
        var path = Path.Combine(Path.GetTempPath(), $"userscript-test-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{ not valid json");

            var store = new JsonUserScriptStore(path);
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
    public void Remove_DeletesEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"userscript-test-{Guid.NewGuid()}.json");
        var store = new JsonUserScriptStore(path);
        var entry = store.Add(new UserScriptEntry { Name = "ToRemove" });

        store.Remove(entry.Id);

        Assert.Empty(store.Items);
    }

    [Fact]
    public void Update_ModifiesExistingEntry()
    {
        var path = Path.Combine(Path.GetTempPath(), $"userscript-test-{Guid.NewGuid()}.json");
        var store = new JsonUserScriptStore(path);
        var entry = store.Add(new UserScriptEntry { Name = "Old", Code = "a();" });
        entry.Name = "New";
        entry.Code = "b();";
        store.Update(entry);

        Assert.Equal("New", store.Items[0].Name);
        Assert.Equal("b();", store.Items[0].Code);
    }
}