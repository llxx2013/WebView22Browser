using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebView22Browser.App.Services;

namespace WebView22Browser.App.ViewModels;

public partial class ExtensionsViewModel : ObservableObject
{
    private readonly BrowserExtensionService _extensionService;
    private readonly IDialogService _dialogService;

    public ExtensionsViewModel(BrowserExtensionService extensionService, IDialogService dialogService)
    {
        _extensionService = extensionService;
        _dialogService = dialogService;
        Items = new ObservableCollection<ExtensionItemViewModel>();
        _extensionService.ExtensionsChanged += OnExtensionsChanged;
    }

    public void Detach() => _extensionService.ExtensionsChanged -= OnExtensionsChanged;

    public ObservableCollection<ExtensionItemViewModel> Items { get; }

    [ObservableProperty]
    private bool _isPanelOpen;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = "仅支持本地解压扩展，不支持应用商店。";

    public void RefreshFromService()
    {
        Items.Clear();
        foreach (var item in _extensionService.Items)
            Items.Add(item);
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private async Task InstallFromFolderAsync()
    {
        if (!_extensionService.IsInitialized)
        {
            _dialogService.ShowWarning("WebView2 尚未就绪，请稍后再试。", "扩展程序");
            return;
        }

        var folder = _dialogService.PickFolder("选择扩展程序文件夹（需包含 manifest.json）");
        if (string.IsNullOrEmpty(folder))
            return;

        IsBusy = true;
        try
        {
            var installed = await _extensionService.InstallFromFolderAsync(folder);
            if (installed != null)
                StatusMessage = $"已安装：{installed.Name}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(ExtensionItemViewModel? item)
    {
        if (item == null)
            return;

        await _extensionService.SetEnabledAsync(item, !item.IsEnabled);
    }

    [RelayCommand]
    private async Task RemoveExtensionAsync(ExtensionItemViewModel? item)
    {
        if (item == null)
            return;

        if (!_dialogService.Confirm($"确定要移除扩展「{item.Name}」吗？", "移除扩展"))
            return;

        IsBusy = true;
        try
        {
            await _extensionService.RemoveAsync(item);
            StatusMessage = "扩展已移除。";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnExtensionsChanged()
    {
        var dispatcher = System.Windows.Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            RefreshFromService();
            return;
        }

        if (dispatcher.CheckAccess())
            RefreshFromService();
        else
            dispatcher.Invoke(RefreshFromService);
    }
}
