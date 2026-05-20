using System.Collections.ObjectModel;
using System.ComponentModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WebView22Browser.App.Services;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private const string WindowTitleSuffix = " - WebView2 Browser";

    private readonly BrowserOptions _options;
    private readonly NavigationService _navigationService;
    private readonly UserScriptService _userScriptService;
    private readonly ITabSessionStore _sessionStore;

    public MainViewModel(
        BrowserOptions options,
        NavigationService navigationService,
        ITabSessionStore sessionStore,
        FavoritesViewModel favorites,
        ExtensionsViewModel extensions,
        UserScriptsViewModel userScripts,
        UserScriptCommandsViewModel scriptCommands,
        UserScriptService userScriptService,
        DownloadsViewModel downloads,
        HistoryViewModel history,
        SettingsViewModel settings)
    {
        _options = options;
        _navigationService = navigationService;
        _sessionStore = sessionStore;
        _userScriptService = userScriptService;
        Favorites = favorites;
        Extensions = extensions;
        UserScripts = userScripts;
        ScriptCommands = scriptCommands;
        Downloads = downloads;
        History = history;
        Settings = settings;
        Tabs = new ObservableCollection<BrowserTabViewModel>();
        History.PropertyChanged += OnOverlayPagePropertyChanged;
        Settings.PropertyChanged += OnOverlayPagePropertyChanged;
        Downloads.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(DownloadsViewModel.ActiveCount))
                UpdateDownloadStatusMessage();
        };
        Downloads.ActiveItems.CollectionChanged += (_, _) => UpdateDownloadStatusMessage();
    }

    public ObservableCollection<BrowserTabViewModel> Tabs { get; }

    public FavoritesViewModel Favorites { get; }

    public ExtensionsViewModel Extensions { get; }

    public UserScriptsViewModel UserScripts { get; }

    public UserScriptCommandsViewModel ScriptCommands { get; }

    public DownloadsViewModel Downloads { get; }

    public HistoryViewModel History { get; }

    public SettingsViewModel Settings { get; }

    public bool IsBrowserContentVisible => !History.IsPageOpen && !Settings.IsPageOpen;

    [ObservableProperty]
    private BrowserTabViewModel? _selectedTab;

    [ObservableProperty]
    private bool _isToolbarEnabled;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string _windowTitle = "WebView2 Browser";

    public event Action<BrowserTabViewModel, string?>? NewTabRequested;

    public event Action<BrowserTabViewModel>? CloseTabRequested;

    public async Task<bool> InitializeAsync()
    {
        await Favorites.LoadAsync();
        await _userScriptService.LoadAsync();
        await UserScripts.LoadAsync();
        await Downloads.LoadHistoryAsync();
        await History.LoadAsync();

        if (_options.RestoreLastSession)
        {
            var session = await _sessionStore.LoadAsync();
            if (session is { Tabs.Count: > 0 })
            {
                RestoreSession(session);
                return true;
            }
        }

        OpenNewTab(_options.HomeUrl);
        return false;
    }

    private void RestoreSession(TabSessionFile session)
    {
        BrowserTabViewModel? selected = null;

        foreach (var snap in session.Tabs)
        {
            var address = ResolveCurrentAddress(snap);
            var isSelectedTab = session.SelectedTabId == snap.TabId;
            var tab = new BrowserTabViewModel
            {
                TabId = snap.TabId,
                Title = string.IsNullOrWhiteSpace(snap.Title) ? "新标签页" : snap.Title,
                Address = address,
                InitialUri = address,
                IsSleeping = !isSelectedTab,
                IsWebViewReady = false
            };

            tab.FrozenHistoryEntries = snap.History.Count > 0 ? snap.History : null;
            tab.FrozenHistoryIndex = snap.CurrentIndex;
            tab.SetLastActiveUtc(snap.LastActiveUtc);

            Tabs.Add(tab);
            NewTabRequested?.Invoke(tab, address);

            if (isSelectedTab)
                selected = tab;
        }

        if (selected == null && Tabs.Count > 0)
            Tabs[0].IsSleeping = false;

        SelectedTab = selected ?? Tabs.FirstOrDefault();
        UpdateTabSelection();
        UpdateWindowTitle();
    }

    private string ResolveCurrentAddress(TabSessionSnapshot snap)
    {
        if (snap.History.Count > 0)
        {
            var index = Math.Clamp(snap.CurrentIndex, 0, snap.History.Count - 1);
            return snap.History[index];
        }

        return string.IsNullOrWhiteSpace(snap.Address) ? _options.HomeUrl : snap.Address;
    }

    public void NavigateToUrl(string url)
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(url))
            return;

        var uri = _navigationService.ResolveUri(url);
        SelectedTab.Address = uri;
        SelectedTab.RequestNavigate(uri);
    }

    private void UpdateDownloadStatusMessage()
    {
        var count = Downloads.ActiveCount;
        StatusMessage = count switch
        {
            0 => string.Empty,
            1 => "1 个下载进行中",
            _ => $"{count} 个下载进行中"
        };
    }

    public BrowserTabViewModel OpenNewTab(string? uri = null)
    {
        var tab = new BrowserTabViewModel
        {
            InitialUri = uri ?? _options.HomeUrl,
            Address = uri ?? string.Empty
        };

        tab.TouchActivity();
        Tabs.Add(tab);
        SelectedTab = tab;
        UpdateTabSelection();
        NewTabRequested?.Invoke(tab, uri ?? _options.HomeUrl);
        UpdateWindowTitle();
        return tab;
    }

    partial void OnSelectedTabChanged(BrowserTabViewModel? value)
    {
        value?.TouchActivity();
        UpdateTabSelection();
        UpdateWindowTitle();
        ScriptCommands.Refresh(value);
        ShowFindCommand.NotifyCanExecuteChanged();
    }

    private void UpdateTabSelection()
    {
        foreach (var tab in Tabs)
            tab.IsSelected = tab == SelectedTab;
    }

    public void UpdateWindowTitle()
    {
        var title = SelectedTab?.Title;
        WindowTitle = string.IsNullOrWhiteSpace(title) || SelectedTab == null
            ? "WebView2 Browser"
            : title + WindowTitleSuffix;
    }

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void Navigate()
    {
        if (SelectedTab == null)
            return;

        var uri = _navigationService.ResolveUri(SelectedTab.Address);
        SelectedTab.RequestNavigate(uri);
    }

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void GoBack() => SelectedTab?.RequestGoBack();

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void GoForward() => SelectedTab?.RequestGoForward();

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void Reload() => SelectedTab?.RequestReload();

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void Stop() => SelectedTab?.RequestStop();

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void GoHome()
    {
        if (SelectedTab == null)
            return;

        SelectedTab.Address = _options.HomeUrl;
        SelectedTab.RequestNavigate(_options.HomeUrl);
    }

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void OpenDevTools() => SelectedTab?.RequestOpenDevTools();

    [RelayCommand(CanExecute = nameof(CanShowFind))]
    private void ShowFind() => SelectedTab?.RequestShowFind();

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private async Task AddFavoriteAsync()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(SelectedTab.Address))
            return;

        await Favorites.AddAsync(SelectedTab.Title, SelectedTab.Address);
        StatusMessage = "已加入收藏";
    }

    [RelayCommand]
    private void NewTab() => OpenNewTab(_options.HomeUrl);

    [RelayCommand(CanExecute = nameof(CanCloseTab))]
    private void CloseTab(BrowserTabViewModel? tab)
    {
        tab ??= SelectedTab;
        if (tab == null)
            return;

        if (Tabs.Count <= 1)
        {
            var oldTab = tab;
            OpenNewTab(_options.HomeUrl);
            CloseTabRequested?.Invoke(oldTab);
            Tabs.Remove(oldTab);
            UpdateWindowTitle();
            return;
        }

        var index = Tabs.IndexOf(tab);
        var wasSelected = ReferenceEquals(SelectedTab, tab);

        CloseTabRequested?.Invoke(tab);
        Tabs.Remove(tab);

        if (wasSelected && Tabs.Count > 0)
        {
            SelectedTab = Tabs[Math.Min(index, Tabs.Count - 1)];
            NotifyTabReadyChanged();
        }

        UpdateWindowTitle();
    }

    [RelayCommand(CanExecute = nameof(CanUseBrowser))]
    private void OpenFavorite(FavoriteItem? item)
    {
        if (item == null || SelectedTab == null)
            return;

        SelectedTab.Address = item.Url;
        SelectedTab.RequestNavigate(item.Url);
    }

    public void SelectNextTab()
    {
        if (Tabs.Count < 2 || SelectedTab == null)
            return;

        var index = (Tabs.IndexOf(SelectedTab) + 1) % Tabs.Count;
        SelectedTab = Tabs[index];
    }

    public void HandleNavigation(string uri, BrowserTabViewModel sourceTab, bool openInNewTab)
    {
        if (openInNewTab)
            OpenNewTab(uri);
        else
            sourceTab.RequestNavigate(uri);
    }

    public void HandleNewWindow(string uri) =>
        OpenNewTab(uri);

    private bool CanUseBrowser() => IsToolbarEnabled && SelectedTab?.IsWebViewReady == true;

    private bool CanShowFind() => IsToolbarEnabled && SelectedTab != null;

    private bool CanCloseTab(BrowserTabViewModel? tab)
    {
        tab ??= SelectedTab;
        return tab != null;
    }

    partial void OnIsToolbarEnabledChanged(bool value)
    {
        ScriptCommands.IsToolbarEnabled = value;
        NavigateCommand.NotifyCanExecuteChanged();
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
        ReloadCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        GoHomeCommand.NotifyCanExecuteChanged();
        OpenDevToolsCommand.NotifyCanExecuteChanged();
        ShowFindCommand.NotifyCanExecuteChanged();
        AddFavoriteCommand.NotifyCanExecuteChanged();
        OpenFavoriteCommand.NotifyCanExecuteChanged();
    }

    private void OnOverlayPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(HistoryViewModel.IsPageOpen)
            && e.PropertyName != nameof(SettingsViewModel.IsPageOpen))
        {
            return;
        }

        if (sender == History && History.IsPageOpen)
            Settings.IsPageOpen = false;
        else if (sender == Settings && Settings.IsPageOpen)
            History.IsPageOpen = false;

        OnPropertyChanged(nameof(IsBrowserContentVisible));
    }

    public void NotifyTabReadyChanged()
    {
        ScriptCommands.Refresh(SelectedTab);
        NavigateCommand.NotifyCanExecuteChanged();
        GoBackCommand.NotifyCanExecuteChanged();
        GoForwardCommand.NotifyCanExecuteChanged();
        ReloadCommand.NotifyCanExecuteChanged();
        StopCommand.NotifyCanExecuteChanged();
        GoHomeCommand.NotifyCanExecuteChanged();
        OpenDevToolsCommand.NotifyCanExecuteChanged();
        ShowFindCommand.NotifyCanExecuteChanged();
        AddFavoriteCommand.NotifyCanExecuteChanged();
        OpenFavoriteCommand.NotifyCanExecuteChanged();
        CloseTabCommand.NotifyCanExecuteChanged();
    }
}