using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class TabNavigationHistoryTests
{
    [Fact]
    public void RecordNavigation_FirstEntry_SetsIndexZero()
    {
        var history = new TabNavigationHistory();
        history.RecordNavigation("https://a.com");

        Assert.Equal(0, history.CurrentIndex);
        Assert.Single(history.Entries);
        Assert.Equal("https://a.com", history.Entries[0]);
    }

    [Fact]
    public void RecordNavigation_NewUrl_TruncatesForwardHistory()
    {
        var history = new TabNavigationHistory();
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://b.com");
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://d.com");

        Assert.Equal(1, history.CurrentIndex);
        Assert.Equal(2, history.Entries.Count);
        Assert.Equal("https://a.com", history.Entries[0]);
        Assert.Equal("https://d.com", history.Entries[1]);
    }

    [Fact]
    public void RecordNavigation_Back_DecrementsIndex()
    {
        var history = new TabNavigationHistory();
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://b.com");
        history.RecordNavigation("https://a.com");

        Assert.Equal(0, history.CurrentIndex);
    }

    [Fact]
    public void RecordNavigation_Forward_IncrementsIndex()
    {
        var history = new TabNavigationHistory();
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://b.com");
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://b.com");

        Assert.Equal(1, history.CurrentIndex);
    }

    [Fact]
    public void RecordNavigation_ReloadSameUrl_DoesNotChangeStack()
    {
        var history = new TabNavigationHistory();
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://b.com");
        history.RecordNavigation("https://b.com");

        Assert.Equal(1, history.CurrentIndex);
        Assert.Equal(2, history.Entries.Count);
    }

    [Fact]
    public void RecordNavigation_ExceedsMax_TrimsOldest()
    {
        var history = new TabNavigationHistory(maxEntries: 3);
        history.RecordNavigation("https://1.com");
        history.RecordNavigation("https://2.com");
        history.RecordNavigation("https://3.com");
        history.RecordNavigation("https://4.com");

        Assert.Equal(3, history.Entries.Count);
        Assert.Equal("https://2.com", history.Entries[0]);
        Assert.Equal("https://4.com", history.Entries[2]);
        Assert.Equal(2, history.CurrentIndex);
    }

    [Fact]
    public void GetSnapshot_ReturnsCopy()
    {
        var history = new TabNavigationHistory();
        history.RecordNavigation("https://a.com");
        history.RecordNavigation("https://b.com");

        var snapshot = history.GetSnapshot();

        Assert.Equal(1, snapshot.CurrentIndex);
        Assert.Equal(2, snapshot.Entries.Count);
        Assert.NotSame(history.Entries, snapshot.Entries);
    }

    [Fact]
    public void RestoreSnapshot_RestoresState()
    {
        var history = new TabNavigationHistory();
        history.RestoreSnapshot(new[] { "https://a.com", "https://b.com", "https://c.com" }, 1);

        Assert.Equal(1, history.CurrentIndex);
        Assert.Equal(3, history.Entries.Count);
        history.RecordNavigation("https://b.com");
        Assert.Equal(1, history.CurrentIndex);
    }
}
