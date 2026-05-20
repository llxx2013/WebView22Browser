using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class TabSessionSnapshotBuilderTests
{
    [Fact]
    public void Build_TrimsHistoryToMaxEntries()
    {
        var tabId = Guid.NewGuid();
        var history = Enumerable.Range(0, 60).Select(i => $"https://example.com/{i}").ToList();

        var file = TabSessionSnapshotBuilder.Build(
            [new TabSessionSource(tabId, "Title", "https://example.com/59", history, 59, DateTime.UtcNow)],
            tabId,
            maxHistoryEntries: 50);

        Assert.Equal(50, file.Tabs[0].History.Count);
        Assert.Equal("https://example.com/10", file.Tabs[0].History[0]);
        Assert.Equal(49, file.Tabs[0].CurrentIndex);
    }

    [Fact]
    public void Build_WhenNoHistory_UsesAddress()
    {
        var tabId = Guid.NewGuid();
        var file = TabSessionSnapshotBuilder.Build(
            [new TabSessionSource(tabId, "T", "https://example.com", null, -1, DateTime.UtcNow)],
            tabId,
            maxHistoryEntries: 50);

        Assert.Single(file.Tabs[0].History);
        Assert.Equal("https://example.com", file.Tabs[0].History[0]);
    }

    [Fact]
    public void Build_WhenMaxEntriesIsOne_KeepsOnlyCurrentUrl()
    {
        var tabId = Guid.NewGuid();
        var history = new List<string> { "https://example.com/a", "https://example.com/b" };

        var file = TabSessionSnapshotBuilder.Build(
            [new TabSessionSource(tabId, "T", "https://example.com/b", history, 1, DateTime.UtcNow)],
            tabId,
            maxHistoryEntries: 1);

        Assert.Single(file.Tabs[0].History);
        Assert.Equal("https://example.com/b", file.Tabs[0].History[0]);
        Assert.Equal(0, file.Tabs[0].CurrentIndex);
    }
}
