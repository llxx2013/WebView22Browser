using System.Diagnostics;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WebView22Browser.App.Services;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly BrowserOptions _options;
    private readonly BrowserAppConfig _appConfig;
    private readonly IUserSettingsStore _settingsStore;
    private readonly IDialogService _dialogService;
    private readonly IRuntimeBrowserSettingsApplier _settingsApplier;
    private readonly IDownloadHistoryStore _downloadHistoryStore;
    private readonly IBrowsingHistoryStore _browsingHistoryStore;
    private readonly IBrowsingHistoryService _browsingHistoryService;

    public SettingsViewModel(
        BrowserOptions options,
        BrowserAppConfig appConfig,
        IUserSettingsStore settingsStore,
        IDialogService dialogService,
        IRuntimeBrowserSettingsApplier settingsApplier,
        IDownloadHistoryStore downloadHistoryStore,
        IBrowsingHistoryStore browsingHistoryStore,
        IBrowsingHistoryService browsingHistoryService)
    {
        _options = options;
        _appConfig = appConfig;
        _settingsStore = settingsStore;
        _dialogService = dialogService;
        _settingsApplier = settingsApplier;
        _downloadHistoryStore = downloadHistoryStore;
        _browsingHistoryStore = browsingHistoryStore;
        _browsingHistoryService = browsingHistoryService;
        LoadFromOptions();
        RefreshDataPaths();
    }

    public IReadOnlyList<SettingsPathItemViewModel> DataPaths { get; private set; } = [];

    [ObservableProperty]
    private bool _isPageOpen;

    [ObservableProperty]
    private string _homeUrl = string.Empty;

    [ObservableProperty]
    private string _searchUrlTemplate = string.Empty;

    [ObservableProperty]
    private bool _restoreLastSession;

    [ObservableProperty]
    private int _tabSleepTimeoutMinutes;

    [ObservableProperty]
    private int _tabSleepCheckIntervalSeconds;

    [ObservableProperty]
    private int _tabHistoryMaxEntries;

    [ObservableProperty]
    private int _pressureElevatedMemoryPercent;

    [ObservableProperty]
    private int _pressureHighMemoryPercent;

    [ObservableProperty]
    private int _pressureHighCpuPercent;

    [ObservableProperty]
    private int _pressureSampleWindowSeconds;

    [ObservableProperty]
    private int _browsingHistoryMaxEntries;

    [ObservableProperty]
    private int _downloadHistoryMaxEntries;

    [ObservableProperty]
    private string? _statusMessage;

    partial void OnIsPageOpenChanged(bool value)
    {
        if (value)
        {
            StatusMessage = null;
            LoadFromOptions();
            RefreshDataPaths();
        }
    }

    [RelayCommand]
    private void TogglePage() => IsPageOpen = !IsPageOpen;

    [RelayCommand]
    private void ClosePage() => IsPageOpen = false;

    [RelayCommand]
    private async Task SaveAsync()
    {
        var dto = BuildDtoFromProperties();
        var validation = BrowserSettingsValidator.Validate(dto);
        if (!validation.IsValid)
        {
            StatusMessage = null;
            _dialogService.ShowError(validation.ErrorMessage, "设置无效");
            return;
        }

        ApplyDtoToOptions(dto);
        await _settingsStore.SaveAsync(dto);
        _settingsApplier.ApplyRuntimeSettings();
        _downloadHistoryStore.TrimToMaxEntries(_options.DownloadHistoryMaxEntries);
        _browsingHistoryStore.TrimToMaxEntries(_options.BrowsingHistoryMaxEntries);
        await _downloadHistoryStore.SaveAsync();
        await _browsingHistoryStore.SaveAsync();
        _browsingHistoryService.NotifyHistoryChanged();
        StatusMessage = "设置已保存";
    }

    [RelayCommand]
    private async Task ResetToDefaultsAsync()
    {
        if (!_dialogService.Confirm(
                "将恢复为 appsettings.json 中的默认值，并删除 user-settings.json。是否继续？",
                "恢复默认设置"))
        {
            return;
        }

        await _settingsStore.ClearAsync();
        var defaults = BrowserOptionsLoader.DefaultsFromAppConfig(_appConfig);
        BrowserOptionsLoader.ApplyUserOverrides(_options, defaults);
        LoadFromPropertiesDto(defaults);
        await _settingsStore.SaveAsync(defaults);
        _settingsApplier.ApplyRuntimeSettings();
        _downloadHistoryStore.TrimToMaxEntries(_options.DownloadHistoryMaxEntries);
        _browsingHistoryStore.TrimToMaxEntries(_options.BrowsingHistoryMaxEntries);
        await _downloadHistoryStore.SaveAsync();
        await _browsingHistoryStore.SaveAsync();
        _browsingHistoryService.NotifyHistoryChanged();
        StatusMessage = "已恢复默认设置";
    }

    [RelayCommand]
    private void OpenDataPath(SettingsPathItemViewModel? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Path))
            return;

        if (item.IsDirectory)
        {
            if (!Directory.Exists(item.Path))
            {
                _dialogService.ShowWarning($"目录不存在：{item.Path}", "无法打开");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = item.Path,
                UseShellExecute = true
            });
            return;
        }

        var directory = Path.GetDirectoryName(item.Path);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            _dialogService.ShowWarning($"路径不存在：{item.Path}", "无法打开");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = File.Exists(item.Path) ? $"/select,\"{item.Path}\"" : $"\"{directory}\"",
            UseShellExecute = true
        });
    }

    private void LoadFromOptions() => LoadFromPropertiesDto(BrowserOptionsLoader.FromOptions(_options));

    private void LoadFromPropertiesDto(BrowserSettingsDto dto)
    {
        HomeUrl = dto.HomeUrl ?? _options.HomeUrl;
        SearchUrlTemplate = dto.SearchUrlTemplate ?? _options.SearchUrlTemplate;
        RestoreLastSession = dto.RestoreLastSession ?? _options.RestoreLastSession;
        TabSleepTimeoutMinutes = dto.TabSleepTimeoutMinutes ?? _options.TabSleepTimeoutMinutes;
        TabSleepCheckIntervalSeconds = dto.TabSleepCheckIntervalSeconds ?? _options.TabSleepCheckIntervalSeconds;
        TabHistoryMaxEntries = dto.TabHistoryMaxEntries ?? _options.TabHistoryMaxEntries;
        PressureElevatedMemoryPercent = dto.PressureElevatedMemoryPercent ?? _options.PressureElevatedMemoryPercent;
        PressureHighMemoryPercent = dto.PressureHighMemoryPercent ?? _options.PressureHighMemoryPercent;
        PressureHighCpuPercent = dto.PressureHighCpuPercent ?? _options.PressureHighCpuPercent;
        PressureSampleWindowSeconds = dto.PressureSampleWindowSeconds ?? _options.PressureSampleWindowSeconds;
        BrowsingHistoryMaxEntries = dto.BrowsingHistoryMaxEntries ?? _options.BrowsingHistoryMaxEntries;
        DownloadHistoryMaxEntries = dto.DownloadHistoryMaxEntries ?? _options.DownloadHistoryMaxEntries;
    }

    private BrowserSettingsDto BuildDtoFromProperties() => new()
    {
        HomeUrl = HomeUrl.Trim(),
        SearchUrlTemplate = SearchUrlTemplate.Trim(),
        RestoreLastSession = RestoreLastSession,
        TabSleepTimeoutMinutes = TabSleepTimeoutMinutes,
        TabSleepCheckIntervalSeconds = TabSleepCheckIntervalSeconds,
        TabHistoryMaxEntries = TabHistoryMaxEntries,
        PressureElevatedMemoryPercent = PressureElevatedMemoryPercent,
        PressureHighMemoryPercent = PressureHighMemoryPercent,
        PressureHighCpuPercent = PressureHighCpuPercent,
        PressureSampleWindowSeconds = PressureSampleWindowSeconds,
        BrowsingHistoryMaxEntries = BrowsingHistoryMaxEntries,
        DownloadHistoryMaxEntries = DownloadHistoryMaxEntries
    };

    private void ApplyDtoToOptions(BrowserSettingsDto dto)
    {
        _options.HomeUrl = dto.HomeUrl ?? _options.HomeUrl;
        _options.SearchUrlTemplate = dto.SearchUrlTemplate ?? _options.SearchUrlTemplate;
        _options.RestoreLastSession = dto.RestoreLastSession ?? _options.RestoreLastSession;
        _options.TabSleepTimeoutMinutes = dto.TabSleepTimeoutMinutes ?? _options.TabSleepTimeoutMinutes;
        _options.TabSleepCheckIntervalSeconds = dto.TabSleepCheckIntervalSeconds ?? _options.TabSleepCheckIntervalSeconds;
        _options.TabHistoryMaxEntries = dto.TabHistoryMaxEntries ?? _options.TabHistoryMaxEntries;
        _options.PressureElevatedMemoryPercent = dto.PressureElevatedMemoryPercent ?? _options.PressureElevatedMemoryPercent;
        _options.PressureHighMemoryPercent = dto.PressureHighMemoryPercent ?? _options.PressureHighMemoryPercent;
        _options.PressureHighCpuPercent = dto.PressureHighCpuPercent ?? _options.PressureHighCpuPercent;
        _options.PressureSampleWindowSeconds = dto.PressureSampleWindowSeconds ?? _options.PressureSampleWindowSeconds;
        _options.BrowsingHistoryMaxEntries = dto.BrowsingHistoryMaxEntries ?? _options.BrowsingHistoryMaxEntries;
        _options.DownloadHistoryMaxEntries = dto.DownloadHistoryMaxEntries ?? _options.DownloadHistoryMaxEntries;
    }

    private void RefreshDataPaths()
    {
        DataPaths =
        [
            new SettingsPathItemViewModel("WebView2 用户数据", _options.GetUserDataFolder(), isDirectory: true),
            new SettingsPathItemViewModel("收藏夹", _options.GetDefaultFavoritesPath(), isDirectory: false),
            new SettingsPathItemViewModel("扩展注册表", _options.GetDefaultExtensionsRegistryPath(), isDirectory: false),
            new SettingsPathItemViewModel("下载历史", _options.GetDefaultDownloadHistoryPath(), isDirectory: false),
            new SettingsPathItemViewModel("浏览历史", _options.GetDefaultBrowsingHistoryPath(), isDirectory: false),
            new SettingsPathItemViewModel("用户脚本", _options.GetDefaultUserScriptsPath(), isDirectory: false),
            new SettingsPathItemViewModel("标签会话", _options.GetDefaultTabSessionPath(), isDirectory: false),
            new SettingsPathItemViewModel("GM 存储", _options.GetDefaultGmStorageDirectory(), isDirectory: true),
            new SettingsPathItemViewModel("用户设置", _settingsStore.FilePath, isDirectory: false)
        ];
        OnPropertyChanged(nameof(DataPaths));
    }
}
