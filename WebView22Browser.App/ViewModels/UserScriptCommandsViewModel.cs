using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class UserScriptCommandsViewModel : ObservableObject
{
    private readonly GmMenuCommandRegistry _registry;
    private readonly ITabHostService _tabHostService;
    private readonly IUserScriptStore _scriptStore;
    private BrowserTabViewModel? _currentTab;

    public UserScriptCommandsViewModel(
        GmMenuCommandRegistry registry,
        ITabHostService tabHostService,
        IUserScriptStore scriptStore)
    {
        _registry = registry;
        _tabHostService = tabHostService;
        _scriptStore = scriptStore;
        Items = new ObservableCollection<UserScriptMenuCommandItemViewModel>();
        _registry.CommandsChanged += OnCommandsChanged;
    }

    public ObservableCollection<UserScriptMenuCommandItemViewModel> Items { get; }

    [ObservableProperty]
    private bool _hasCommands;

    [ObservableProperty]
    private bool _isToolbarEnabled;

    public bool IsMenuEnabled => HasCommands && IsToolbarEnabled;

    public void Refresh(BrowserTabViewModel? tab)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            RefreshCore(tab);
            return;
        }

        dispatcher.Invoke(() => RefreshCore(tab), DispatcherPriority.Normal);
    }

    private void RefreshCore(BrowserTabViewModel? tab)
    {
        _currentTab = tab;
        Items.Clear();

        if (tab == null || !tab.IsWebViewReady)
        {
            HasCommands = false;
            OnPropertyChanged(nameof(IsMenuEnabled));
            return;
        }

        var host = _tabHostService.GetHost(tab.TabId);
        var core = host?.CoreWebView2;
        if (core == null)
        {
            HasCommands = false;
            OnPropertyChanged(nameof(IsMenuEnabled));
            return;
        }

        foreach (var command in _registry.GetCommands(core))
        {
            var script = _scriptStore.FindById(command.ScriptId);
            var scriptName = script?.Name ?? command.ScriptId.ToString();
            var display = $"{scriptName} › {command.Caption}";
            if (!string.IsNullOrEmpty(command.AccessKey))
                display += $" ({command.AccessKey})";

            Items.Add(new UserScriptMenuCommandItemViewModel
            {
                CommandId = command.CommandId,
                ScriptId = command.ScriptId,
                DisplayName = display
            });
        }

        HasCommands = Items.Count > 0;
        OnPropertyChanged(nameof(IsMenuEnabled));
    }

    partial void OnHasCommandsChanged(bool value) => OnPropertyChanged(nameof(IsMenuEnabled));

    partial void OnIsToolbarEnabledChanged(bool value) => OnPropertyChanged(nameof(IsMenuEnabled));

    [RelayCommand]
    private async Task RunMenuCommandAsync(UserScriptMenuCommandItemViewModel? item)
    {
        if (item == null || _currentTab == null || !_currentTab.IsWebViewReady)
            return;

        var host = _tabHostService.GetHost(_currentTab.TabId);
        if (host?.CoreWebView2 is not { } core)
            return;

        await _registry.InvokeAsync(core, item.CommandId, item.ScriptId);
    }

    private void OnCommandsChanged(object? sender, CoreWebView2 core)
    {
        var tab = _currentTab;
        if (tab == null || !tab.IsWebViewReady)
            return;

        var host = _tabHostService.GetHost(tab.TabId);
        if (host?.CoreWebView2 is not { } currentCore || !ReferenceEquals(currentCore, core))
            return;

        Refresh(tab);
    }
}