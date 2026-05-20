using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonDownloadHistoryStoreTests
{
    [Fact]
    public async Task SaveAndLoad_RoundTripsItems()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dl-test-{Guid.NewGuid()}.json");
        try
        {
            var store = new JsonDownloadHistoryStore(path);
            store.Upsert(new DownloadHistoryEntry
            {
                FileName = "file.zip",
                SourceUrl = "https://example.com/file.zip",
                FullPath = @"C:\temp\file.zip",
                State = DownloadState.Completed,
                BytesReceived = 100,
                TotalBytes = 100
            });
            await store.SaveAsync();

            var reloaded = new JsonDownloadHistoryStore(path);
            await reloaded.LoadAsync();

            Assert.Single(reloaded.Items);
            Assert.Equal("file.zip", reloaded.Items[0].FileName);
            Assert.Equal(DownloadState.Completed, reloaded.Items[0].State);
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
        var path = Path.Combine(Path.GetTempPath(), $"dl-test-{Guid.NewGuid()}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{ not valid");

            var store = new JsonDownloadHistoryStore(path);
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
    public void TrimToMaxEntries_KeepsNewestCompleted()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dl-test-{Guid.NewGuid()}.json");
        var store = new JsonDownloadHistoryStore(path);

        for (var i = 0; i < 5; i++)
        {
            store.Upsert(new DownloadHistoryEntry
            {
                FileName = $"file{i}.bin",
                State = DownloadState.Completed,
                CompletedAtUtc = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        store.TrimToMaxEntries(3);
        Assert.Equal(3, store.Items.Count);
    }

    [Fact]
    public void ClearCompleted_RemovesFinishedItems()
    {
        var store = new JsonDownloadHistoryStore(Path.Combine(Path.GetTempPath(), $"dl-test-{Guid.NewGuid()}.json"));
        store.Upsert(new DownloadHistoryEntry { State = DownloadState.Completed });
        store.Upsert(new DownloadHistoryEntry { State = DownloadState.Interrupted });

        store.ClearCompleted();

        Assert.Single(store.Items);
        Assert.Equal(DownloadState.Interrupted, store.Items[0].State);
    }
}