using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonBrowsingHistoryStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsItems()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bh-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonBrowsingHistoryStore(path);
            store.Add(new BrowsingHistoryEntry
            {
                Url = "https://example.com",
                Title = "Example",
                VisitedAtUtc = DateTime.UtcNow
            });
            await store.SaveAsync();

            var reloaded = new JsonBrowsingHistoryStore(path);
            await reloaded.LoadAsync();

            Assert.Single(reloaded.Items);
            Assert.Equal("https://example.com", reloaded.Items[0].Url);
            Assert.Equal("Example", reloaded.Items[0].Title);
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
        var path = Path.Combine(Path.GetTempPath(), $"bh-test-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{ not valid");

            var store = new JsonBrowsingHistoryStore(path);
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
    public void TrimToMaxEntries_KeepsNewest()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bh-test-{Guid.NewGuid()}.json");
        var store = new JsonBrowsingHistoryStore(path);

        for (var i = 0; i < 5; i++)
        {
            store.Add(new BrowsingHistoryEntry
            {
                Url = $"https://example.com/{i}",
                Title = $"Page {i}",
                VisitedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        store.TrimToMaxEntries(3);
        Assert.Equal(3, store.Items.Count);
        Assert.Equal("https://example.com/4", store.Items[0].Url);
    }

    [Fact]
    public void Clear_RemovesAll()
    {
        var store = new JsonBrowsingHistoryStore(Path.Combine(Path.GetTempPath(), $"bh-test-{Guid.NewGuid()}.json"));
        store.Add(new BrowsingHistoryEntry { Url = "https://a.com" });
        store.Clear();
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task Add_Concurrent_DoesNotThrowAndPreservesCount()
    {
        var path = Path.Combine(Path.GetTempPath(), $"bh-test-{Guid.NewGuid()}.json");
        var store = new JsonBrowsingHistoryStore(path);

        var tasks = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => store.Add(new BrowsingHistoryEntry
            {
                Url = $"https://example.com/{i}",
                Title = $"Page {i}"
            })))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(50, store.Items.Count);
    }

    [Fact]
    public void AddAndTrimToMax_EnforcesLimitAtomically()
    {
        var store = new JsonBrowsingHistoryStore(Path.Combine(Path.GetTempPath(), $"bh-test-{Guid.NewGuid()}.json"));

        for (var i = 0; i < 5; i++)
        {
            store.AddAndTrimToMax(new BrowsingHistoryEntry
            {
                Url = $"https://example.com/{i}",
                Title = $"Page {i}"
            }, maxEntries: 3);
        }

        Assert.Equal(3, store.Items.Count);
        Assert.Equal("https://example.com/4", store.Items[0].Url);
    }
}