using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonTabSessionStoreTests
{
    [Fact]
    public async Task LoadSave_RoundTripsSession()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tabs-session-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonTabSessionStore(path);
            var session = new TabSessionFile
            {
                Version = 1,
                SelectedTabId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Tabs =
                [
                    new TabSessionSnapshot
                    {
                        TabId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                        Title = "Example",
                        Address = "https://example.com",
                        History = ["https://example.com", "https://example.com/page"],
                        CurrentIndex = 1,
                        LastActiveUtc = DateTime.UtcNow
                    }
                ]
            };

            store.Update(session);
            await store.FlushAsync();

            var loaded = await store.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Equal(1, loaded!.Version);
            Assert.Single(loaded.Tabs);
            Assert.Equal("Example", loaded.Tabs[0].Title);
            Assert.Equal(1, loaded.Tabs[0].CurrentIndex);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
            var tmp = path + ".tmp";
            if (File.Exists(tmp))
                File.Delete(tmp);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenJsonCorrupt_ReturnsNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tabs-session-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{ not valid json");
            var store = new JsonTabSessionStore(path);
            var loaded = await store.LoadAsync();
            Assert.Null(loaded);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task FlushAsync_WritesTempFileThenMoves()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tabs-session-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonTabSessionStore(path);
            store.Update(new TabSessionFile
            {
                Version = 1,
                Tabs = [new TabSessionSnapshot { TabId = Guid.NewGuid(), Title = "T", Address = "https://a.com" }]
            });
            await store.FlushAsync();

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task Update_ThenFlushIfDebounced_WithinInterval_SkipsWrite()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tabs-session-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonTabSessionStore(path);
            store.Update(new TabSessionFile
            {
                Version = 1,
                Tabs = [new TabSessionSnapshot { TabId = Guid.NewGuid(), Title = "A", Address = "https://a.com" }]
            });
            await store.FlushAsync();

            store.Update(new TabSessionFile
            {
                Version = 1,
                Tabs = [new TabSessionSnapshot { TabId = Guid.NewGuid(), Title = "B", Address = "https://b.com" }]
            });
            await store.FlushIfDebouncedAsync();

            var loaded = await store.LoadAsync();
            Assert.NotNull(loaded);
            Assert.Equal("A", loaded!.Tabs[0].Title);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}