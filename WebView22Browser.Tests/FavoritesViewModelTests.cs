using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Models;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

public class FavoritesViewModelTests
{
    [Fact]
    public void TogglePanel_TogglesIsPanelOpen()
    {
        var vm = new FavoritesViewModel(new FakeFavoritesStore());
        Assert.False(vm.IsPanelOpen);

        vm.TogglePanelCommand.Execute(null);
        Assert.True(vm.IsPanelOpen);

        vm.TogglePanelCommand.Execute(null);
        Assert.False(vm.IsPanelOpen);
    }

    [Fact]
    public async Task AddAsync_AddsToCollection()
    {
        var store = new FakeFavoritesStore();
        var vm = new FavoritesViewModel(store);

        await vm.AddAsync("Test", "https://example.com");

        Assert.Single(vm.Items);
        Assert.Equal("Test", vm.Items[0].Title);
    }

    [Fact]
    public async Task RemoveAsync_RemovesFromCollection()
    {
        var store = new FakeFavoritesStore();
        var item = store.AddOrUpdate("Test", "https://example.com");
        var vm = new FavoritesViewModel(store);
        await vm.LoadAsync();

        await vm.RemoveCommand.ExecuteAsync(item);

        Assert.Empty(vm.Items);
    }

    [Fact]
    public void OpenFavorite_InvokesNavigateCallback()
    {
        var vm = new FavoritesViewModel(new FakeFavoritesStore());
        FavoriteItem? navigated = null;
        vm.NavigateToFavorite = item => navigated = item;

        var item = new FavoriteItem { Title = "A", Url = "https://a.com" };
        vm.OpenFavoriteCommand.Execute(item);

        Assert.Same(item, navigated);
    }
}
