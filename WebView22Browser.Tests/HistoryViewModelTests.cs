using WebView22Browser.App.Services;
using WebView22Browser.App.ViewModels;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

public class HistoryViewModelTests
{
    private static HistoryViewModel CreateSut(FakeBrowsingHistoryStore store) =>
        new(store, new BrowsingHistoryService(store, new BrowserOptions()), TimeSpan.Zero);

    [Fact]
    public async Task LoadAsync_PopulatesGroupedEntries()
    {
        var store = new FakeBrowsingHistoryStore();
        store.Add(new BrowsingHistoryEntry { Url = "https://example.com", Title = "Example" });

        var vm = CreateSut(store);
        await vm.LoadAsync();

        Assert.False(vm.HasNoResults);
        Assert.Single(vm.GroupedEntries.SelectMany(g => g.Items));
    }

    [Fact]
    public async Task SearchQuery_FiltersEntries()
    {
        var store = new FakeBrowsingHistoryStore();
        store.Add(new BrowsingHistoryEntry { Url = "https://github.com", Title = "GitHub" });
        store.Add(new BrowsingHistoryEntry { Url = "https://bing.com", Title = "Bing" });

        var vm = CreateSut(store);
        await vm.LoadAsync();

        vm.SearchQuery = "github";

        var count = vm.GroupedEntries.Sum(g => g.Items.Count);
        Assert.Equal(1, count);
        Assert.Equal("GitHub", vm.GroupedEntries.SelectMany(g => g.Items).First().Title);
    }

    [Fact]
    public async Task SearchQuery_IsCaseInsensitive()
    {
        var store = new FakeBrowsingHistoryStore();
        store.Add(new BrowsingHistoryEntry { Url = "https://github.com", Title = "GitHub" });

        var vm = CreateSut(store);
        await vm.LoadAsync();

        vm.SearchQuery = "GITHUB";

        Assert.Single(vm.GroupedEntries.SelectMany(g => g.Items));
    }

    [Fact]
    public async Task SearchQuery_NoMatch_SetsHasNoResults()
    {
        var store = new FakeBrowsingHistoryStore();
        store.Add(new BrowsingHistoryEntry { Url = "https://example.com", Title = "Example" });

        var vm = CreateSut(store);
        await vm.LoadAsync();

        vm.SearchQuery = "nomatch";

        Assert.True(vm.HasNoResults);
    }

    [Fact]
    public async Task ClearAll_RemovesAllGroups()
    {
        var store = new FakeBrowsingHistoryStore();
        store.Add(new BrowsingHistoryEntry { Url = "https://example.com", Title = "Example" });

        var vm = CreateSut(store);
        await vm.LoadAsync();

        await vm.ClearAllCommand.ExecuteAsync(null);

        Assert.True(vm.HasNoResults);
        Assert.Empty(store.Items);
    }

    [Fact]
    public async Task RemoveEntry_RemovesSingleItem()
    {
        var store = new FakeBrowsingHistoryStore();
        var entry = new BrowsingHistoryEntry { Url = "https://a.com", Title = "A" };
        store.Add(entry);
        store.Add(new BrowsingHistoryEntry { Url = "https://b.com", Title = "B" });

        var vm = CreateSut(store);
        await vm.LoadAsync();
        var toRemove = vm.GroupedEntries.SelectMany(g => g.Items).First(i => i.Url == "https://a.com");

        await vm.RemoveEntryCommand.ExecuteAsync(toRemove);

        Assert.Single(store.Items);
        Assert.DoesNotContain(vm.GroupedEntries.SelectMany(g => g.Items), i => i.Url == "https://a.com");
    }

    [Fact]
    public void TogglePage_TogglesIsPageOpen()
    {
        var vm = CreateSut(new FakeBrowsingHistoryStore());

        vm.TogglePageCommand.Execute(null);
        Assert.True(vm.IsPageOpen);

        vm.TogglePageCommand.Execute(null);
        Assert.False(vm.IsPageOpen);
    }

    [Fact]
    public void ClosePage_SetsIsPageOpenFalse()
    {
        var vm = CreateSut(new FakeBrowsingHistoryStore());
        vm.IsPageOpen = true;

        vm.ClosePageCommand.Execute(null);

        Assert.False(vm.IsPageOpen);
    }

    [Fact]
    public void OpenEntry_InvokesNavigateAndClosesPage()
    {
        var vm = CreateSut(new FakeBrowsingHistoryStore());
        vm.IsPageOpen = true;
        string? navigated = null;
        vm.NavigateToUrl = url => navigated = url;

        var entry = new HistoryEntryViewModel(new BrowsingHistoryEntry
        {
            Url = "https://example.com",
            Title = "Example"
        });

        vm.OpenEntryCommand.Execute(entry);

        Assert.Equal("https://example.com", navigated);
        Assert.False(vm.IsPageOpen);
    }

    [Fact]
    public async Task IsPageOpen_WhenSetTrue_RefreshesGroups()
    {
        var store = new FakeBrowsingHistoryStore();
        var vm = CreateSut(store);
        await vm.LoadAsync();

        store.Add(new BrowsingHistoryEntry { Url = "https://new.com", Title = "New" });
        vm.IsPageOpen = true;

        Assert.Contains(
            vm.GroupedEntries.SelectMany(g => g.Items),
            i => i.Url == "https://new.com");
    }

    [Fact]
    public void EntryViewModel_UsesHostFallback_WhenTitleEmpty()
    {
        var entry = new HistoryEntryViewModel(new BrowsingHistoryEntry
        {
            Url = "https://example.com/path",
            Title = string.Empty
        });

        Assert.Equal("example.com", entry.Title);
    }
}
