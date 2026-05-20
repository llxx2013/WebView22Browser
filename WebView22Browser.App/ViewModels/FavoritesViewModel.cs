using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class FavoritesViewModel : ObservableObject
{
    private readonly IFavoritesStore _store;

    public FavoritesViewModel(IFavoritesStore store)
    {
        _store = store;
        Items = new ObservableCollection<FavoriteItem>();
    }

    public ObservableCollection<FavoriteItem> Items { get; }

    public Action<FavoriteItem>? NavigateToFavorite { get; set; }

    [ObservableProperty]
    private bool _isPanelOpen;

    public async Task LoadAsync()
    {
        await _store.LoadAsync();
        Items.Clear();
        foreach (var item in _store.Items)
            Items.Add(item);
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private void OpenFavorite(FavoriteItem? item)
    {
        if (item != null)
            NavigateToFavorite?.Invoke(item);
    }

    [RelayCommand]
    private async Task RemoveAsync(FavoriteItem? item)
    {
        if (item == null)
            return;

        _store.Remove(item.Id);
        Items.Remove(item);
        await _store.SaveAsync();
    }

    public async Task AddAsync(string title, string url)
    {
        var item = _store.AddOrUpdate(title, url);
        var existing = Items.FirstOrDefault(i => i.Id == item.Id);
        if (existing != null)
        {
            existing.Title = item.Title;
        }
        else
        {
            Items.Add(item);
        }

        await _store.SaveAsync();
    }
}