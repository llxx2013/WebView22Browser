using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WebView22Browser.App.Services;
using WebView22Browser.App.Views;
using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.ViewModels;

public partial class UserScriptsViewModel : ObservableObject
{
    private readonly IUserScriptStore _store;
    private readonly UserScriptService _userScriptService;
    private readonly UserScriptImportService _importService;
    private readonly UserScriptExtensionConflictService _conflictService;
    private readonly IUserScriptDependencyResolver _dependencyResolver;
    private readonly BrowserOptions _options;
    private readonly IDialogService _dialogService;
    private readonly Lazy<MainViewModel> _mainViewModel;

    public UserScriptsViewModel(
        IUserScriptStore store,
        UserScriptService userScriptService,
        UserScriptImportService importService,
        UserScriptExtensionConflictService conflictService,
        IUserScriptDependencyResolver dependencyResolver,
        BrowserOptions options,
        IDialogService dialogService,
        Lazy<MainViewModel> mainViewModel)
    {
        _store = store;
        _userScriptService = userScriptService;
        _importService = importService;
        _conflictService = conflictService;
        _dependencyResolver = dependencyResolver;
        _options = options;
        _dialogService = dialogService;
        _mainViewModel = mainViewModel;
        Items = new ObservableCollection<UserScriptItemViewModel>();
    }

    public ObservableCollection<UserScriptItemViewModel> Items { get; }

    [ObservableProperty]
    private bool _isPanelOpen;

    [ObservableProperty]
    private string _statusMessage = "支持 .user.js 导入；修改后请刷新页面。";

    public async Task LoadAsync()
    {
        Items.Clear();
        foreach (var entry in _store.Items)
            Items.Add(new UserScriptItemViewModel(entry));

        await RefreshDependencySummariesAsync();
    }

    [RelayCommand]
    private void TogglePanel() => IsPanelOpen = !IsPanelOpen;

    [RelayCommand]
    private async Task AddAsync()
    {
        var entry = new UserScriptEntry
        {
            Name = "新脚本",
            MatchPatterns = ["*://*/*"],
            Code = "// 在此编写 JavaScript\nconsole.log('[userscript] loaded');",
            Enabled = true
        };

        if (!ShowEditDialog(entry, isNew: true))
            return;

        _store.Add(entry);
        Items.Add(new UserScriptItemViewModel(entry));
        await SaveAndRefreshAsync("已添加用户脚本");
    }

    [RelayCommand]
    private async Task ImportFromFileAsync()
    {
        var path = _dialogService.PickOpenFile("导入用户脚本", "UserScript (*.user.js)|*.user.js|所有文件|*.*");
        if (string.IsNullOrEmpty(path))
            return;

        var entry = await _importService.ImportFromFileAsync(path);
        if (entry == null)
            return;

        _store.Add(entry);
        Items.Add(new UserScriptItemViewModel(entry));
        await SaveAndRefreshAsync($"已导入：{entry.Name}");
    }

    [RelayCommand]
    private void CheckConflicts()
    {
        var allPatterns = Items.SelectMany(i => i.Entry.MatchPatterns).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (allPatterns.Count == 0)
        {
            _dialogService.ShowInfo("当前没有可检查的匹配规则。", "扩展冲突");
            return;
        }

        var check = _conflictService.CheckConflictsForPatterns(allPatterns);
        if (!check.IsAvailable)
        {
            _dialogService.ShowWarning(check.UnavailableReason!, "扩展冲突");
            return;
        }

        if (check.Conflicts.Count == 0)
        {
            _dialogService.ShowInfo("未发现与已启用扩展的 URL 重叠。", "扩展冲突");
            return;
        }

        _dialogService.ShowWarning(
            UserScriptExtensionConflictService.FormatConflictMessage(check.Conflicts),
            "扩展冲突");
    }

    [RelayCommand]
    private async Task EditAsync(UserScriptItemViewModel? item)
    {
        if (item == null)
            return;

        var clone = CloneEntry(item.Entry);
        if (!ShowEditDialog(clone, isNew: false))
            return;

        item.Entry.CopyFrom(clone);
        item.Entry.Id = clone.Id;
        item.Entry.CreatedAtUtc = clone.CreatedAtUtc;
        _store.Update(item.Entry);
        item.Refresh();
        await SaveAndRefreshAsync("已更新用户脚本");
    }

    [RelayCommand]
    private async Task RemoveAsync(UserScriptItemViewModel? item)
    {
        if (item == null)
            return;

        if (!_dialogService.Confirm($"确定删除脚本「{item.Name}」？", "用户脚本"))
            return;

        var scriptId = item.Id;
        _store.Remove(scriptId);
        Items.Remove(item);
        await _userScriptService.DeleteScriptStorageAsync(scriptId);
        await SaveAndRefreshAsync("已删除用户脚本");
    }

    [RelayCommand]
    private async Task ToggleEnabledAsync(UserScriptItemViewModel? item)
    {
        if (item == null)
            return;

        var enabling = !item.Entry.Enabled;
        if (enabling)
        {
            var check = _conflictService.CheckConflictsForScript(item.Entry);
            if (!check.IsAvailable)
            {
                _dialogService.ShowWarning(check.UnavailableReason!, "扩展冲突");
            }
            else if (check.Conflicts.Count > 0)
            {
                var message = UserScriptExtensionConflictService.FormatConflictMessage(check.Conflicts) +
                              Environment.NewLine + Environment.NewLine + "是否仍要启用此脚本？";
                if (!_dialogService.Confirm(message, "扩展冲突"))
                    return;
            }
        }

        item.Entry.Enabled = enabling;
        _store.Update(item.Entry);
        item.Refresh();
        await SaveAndRefreshAsync(enabling ? "已启用" : "已禁用");
    }

    [RelayCommand]
    private async Task ReloadAsync() =>
        await SaveAndRefreshAsync("已重新加载用户脚本");

    [RelayCommand]
    private void ReloadAllOpenTabs()
    {
        var tabs = _mainViewModel.Value.Tabs;
        if (tabs.Count == 0)
        {
            StatusMessage = "没有已打开的标签页可刷新。";
            return;
        }

        foreach (var tab in tabs)
            tab.RequestReload();

        StatusMessage = $"已请求刷新 {tabs.Count} 个标签页。";
        Debug.WriteLine($"[userscript] reload-all-tabs requested count={tabs.Count}");
    }

    [RelayCommand]
    private void OpenConfigFolder()
    {
        var folder = Path.GetDirectoryName(_options.GetDefaultUserScriptsPath());
        if (string.IsNullOrEmpty(folder))
            return;

        Directory.CreateDirectory(folder);
        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"\"{folder}\"",
            UseShellExecute = true
        });
    }

    private async Task SaveAndRefreshAsync(string actionMessage)
    {
        await _store.SaveAsync();
        var resolvedByScriptId = await _userScriptService.RefreshAllHostsAsync();
        await ApplyDependencySummariesAsync(resolvedByScriptId);

        var dependencyWarnings = Items
            .Where(i => i.Entry.Enabled)
            .Select(i => resolvedByScriptId.TryGetValue(i.Id, out var deps) ? deps : null)
            .Where(deps => deps != null)
            .SelectMany(deps => deps!.Warnings)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var hasDependencyIssues = Items.Any(i =>
            i.Entry.Enabled
            && UserScriptDependencyStatus.HasDependencies(i.Entry)
            && (!resolvedByScriptId.TryGetValue(i.Id, out var deps)
                || !UserScriptDependencyStatus.IsCacheReady(i.Entry, deps)));

        if (dependencyWarnings.Count > 0)
        {
            StatusMessage =
                $"{actionMessage}。部分 @require/@resource 未能加载（已保存）；请检查网络或 URL 后点击「重载」，并刷新已打开标签。";
            if (dependencyWarnings.Count <= 3)
            {
                StatusMessage += Environment.NewLine + string.Join(
                    Environment.NewLine,
                    dependencyWarnings.Take(3));
            }
        }
        else if (hasDependencyIssues)
        {
            StatusMessage = $"{actionMessage}。依赖尚未全部就绪，请点击「重载」后刷新标签。";
        }
        else
        {
            StatusMessage = Items.Any(i => UserScriptDependencyStatus.HasDependencies(i.Entry))
                ? $"{actionMessage}。依赖已就绪；请刷新已打开标签页，或使用「刷新全部标签」使脚本生效。"
                : $"{actionMessage}。请刷新已打开标签页，或使用「刷新全部标签」使脚本生效。";
        }
    }

    private async Task RefreshDependencySummariesAsync()
    {
        var resolved = new Dictionary<Guid, ResolvedScriptDependencies>();
        foreach (var item in Items)
        {
            if (!item.Entry.Enabled)
            {
                item.UpdateDependencyStatus(null);
                continue;
            }

            var deps = await _dependencyResolver.ResolveAsync(item.Entry);
            resolved[item.Id] = deps;
            item.UpdateDependencyStatus(deps);
        }
    }

    private async Task ApplyDependencySummariesAsync(
        IReadOnlyDictionary<Guid, ResolvedScriptDependencies> resolvedByScriptId)
    {
        foreach (var item in Items)
        {
            if (!item.Entry.Enabled)
            {
                item.UpdateDependencyStatus(null);
                continue;
            }

            if (resolvedByScriptId.TryGetValue(item.Id, out var resolved))
                item.UpdateDependencyStatus(resolved);
            else
                item.UpdateDependencyStatus(await _dependencyResolver.ResolveAsync(item.Entry));
        }
    }

    private bool ShowEditDialog(UserScriptEntry entry, bool isNew)
    {
        var window = new UserScriptEditWindow(entry, isNew, _dialogService);
        return window.ShowDialog() == true;
    }

    private static UserScriptEntry CloneEntry(UserScriptEntry source) =>
        new()
        {
            Id = source.Id,
            Name = source.Name,
            Enabled = source.Enabled,
            MatchPatterns = source.MatchPatterns.ToArray(),
            ExcludePatterns = source.ExcludePatterns.ToArray(),
            Code = source.Code,
            RunAt = source.RunAt,
            RunInTopFrameOnly = source.RunInTopFrameOnly,
            Grants = source.Grants.ToArray(),
            ConnectPatterns = source.ConnectPatterns.ToArray(),
            RequireUrls = source.RequireUrls.ToArray(),
            Resources = new Dictionary<string, string>(source.Resources, StringComparer.Ordinal),
            SourceFilePath = source.SourceFilePath,
            CreatedAtUtc = source.CreatedAtUtc,
            UpdatedAtUtc = source.UpdatedAtUtc
        };
}
