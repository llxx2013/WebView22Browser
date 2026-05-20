using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class BrowserExtensionService
{
    private readonly IExtensionSourceStore _sourceStore;
    private readonly IDialogService _dialogService;
    private readonly object _sync = new();
    private CoreWebView2Profile? _profile;
    private bool _initialized;
    private Task? _initializeTask;
    private List<ExtensionItemViewModel> _items = [];

    public BrowserExtensionService(IExtensionSourceStore sourceStore, IDialogService dialogService)
    {
        _sourceStore = sourceStore;
        _dialogService = dialogService;
    }

    public event Action? ExtensionsChanged;

    public bool IsInitialized => _initialized;

    public IReadOnlyList<ExtensionItemViewModel> Items
    {
        get
        {
            lock (_sync)
                return _items.ToList();
        }
    }

    public Task InitializeAsync(CoreWebView2Profile profile, CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_initialized)
                return Task.CompletedTask;

            _initializeTask ??= InitializeCoreAsync(profile, cancellationToken);
            return _initializeTask;
        }
    }

    private async Task InitializeCoreAsync(CoreWebView2Profile profile, CancellationToken cancellationToken)
    {
        lock (_sync)
            _profile = profile;

        try
        {
            await _sourceStore.LoadAsync(cancellationToken);
            var reinstallFailures = await ReinstallMissingFromStoreAsync(cancellationToken);
            await RefreshAsync(cancellationToken);

            lock (_sync)
                _initialized = true;

            ExtensionsChanged?.Invoke();

            if (reinstallFailures.Count > 0)
                NotifyReinstallFailures(reinstallFailures);
        }
        catch
        {
            lock (_sync)
                _initializeTask = null;
            throw;
        }
    }

    public async Task<ExtensionItemViewModel?> InstallFromFolderAsync(string folderPath, CancellationToken cancellationToken = default)
    {
        EnsureReady();

        if (!ExtensionPathValidator.IsValidExtensionFolder(folderPath, out var validationError))
        {
            _dialogService.ShowError(validationError!, "安装扩展");
            return null;
        }

        var normalizedPath = Path.GetFullPath(folderPath.Trim());

        try
        {
            var extension = await _profile!.AddBrowserExtensionAsync(normalizedPath);
            var entry = _sourceStore.Add(normalizedPath, extension.Id);
            entry.LastKnownExtensionId = extension.Id;
            await _sourceStore.SaveAsync(cancellationToken);
            await RefreshAsync(cancellationToken);
            ExtensionsChanged?.Invoke();
            return FindItem(extension.Id);
        }
        catch (Exception ex) when (IsExtensionError(ex))
        {
            _dialogService.ShowError(MapExtensionError(ex), "安装扩展");
            return null;
        }
    }

    public async Task SetEnabledAsync(ExtensionItemViewModel item, bool enabled, CancellationToken cancellationToken = default)
    {
        EnsureReady();

        try
        {
            await item.Extension.EnableAsync(enabled);
            item.IsEnabled = enabled;
            ExtensionsChanged?.Invoke();
        }
        catch (Exception ex) when (IsExtensionError(ex))
        {
            _dialogService.ShowError(MapExtensionError(ex), "扩展程序");
        }
    }

    public async Task RemoveAsync(ExtensionItemViewModel item, CancellationToken cancellationToken = default)
    {
        EnsureReady();

        try
        {
            await item.Extension.RemoveAsync();
            _sourceStore.RemoveByExtensionId(item.Id);
            await _sourceStore.SaveAsync(cancellationToken);
            await RefreshAsync(cancellationToken);
            ExtensionsChanged?.Invoke();
        }
        catch (Exception ex) when (IsExtensionError(ex))
        {
            _dialogService.ShowError(MapExtensionError(ex), "移除扩展");
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (_profile == null)
            return;

        var extensions = await _profile.GetBrowserExtensionsAsync();
        var items = new List<ExtensionItemViewModel>();
        foreach (var extension in extensions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = _sourceStore.FindByExtensionId(extension.Id)?.FolderPath;
            items.Add(new ExtensionItemViewModel(extension, source));
        }

        lock (_sync)
            _items = items;
    }

    private async Task<List<string>> ReinstallMissingFromStoreAsync(CancellationToken cancellationToken)
    {
        var failures = new List<string>();

        if (_profile == null)
            return failures;

        var installed = await _profile.GetBrowserExtensionsAsync();
        var installedIds = installed.Select(e => e.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in _sourceStore.Items.ToList())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (entry.LastKnownExtensionId != null &&
                installedIds.Contains(entry.LastKnownExtensionId))
                continue;

            if (!ExtensionPathValidator.IsValidExtensionFolder(entry.FolderPath, out _))
            {
                failures.Add(entry.FolderPath);
                continue;
            }

            try
            {
                var extension = await _profile.AddBrowserExtensionAsync(entry.FolderPath);
                entry.LastKnownExtensionId = extension.Id;
                installedIds.Add(extension.Id);
            }
            catch (Exception ex) when (IsExtensionError(ex))
            {
                failures.Add(entry.FolderPath);
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to reinstall extension from {entry.FolderPath}: {ex.Message}");
            }
        }

        await _sourceStore.SaveAsync(cancellationToken);
        return failures;
    }

    private void NotifyReinstallFailures(IReadOnlyList<string> folderPaths)
    {
        var builder = new StringBuilder();
        builder.AppendLine("以下扩展未能自动加载，请检查路径或重新安装：");
        foreach (var path in folderPaths.Take(5))
            builder.AppendLine($"• {path}");
        if (folderPaths.Count > 5)
            builder.AppendLine($"... 另有 {folderPaths.Count - 5} 项");

        _dialogService.ShowWarning(builder.ToString().TrimEnd(), "扩展程序");
    }

    private ExtensionItemViewModel? FindItem(string extensionId)
    {
        lock (_sync)
            return _items.FirstOrDefault(i =>
                string.Equals(i.Id, extensionId, StringComparison.OrdinalIgnoreCase));
    }

    private void EnsureReady()
    {
        if (!_initialized || _profile == null)
            throw new InvalidOperationException("扩展服务尚未初始化，请等待 WebView2 就绪。");
    }

    private static bool IsExtensionError(Exception ex) =>
        ex is COMException or InvalidOperationException or UnauthorizedAccessException;

    private static string MapExtensionError(Exception ex)
    {
        if (ex is COMException com)
        {
            return com.HResult switch
            {
                unchecked((int)0x80070002) => "未找到有效的 manifest.json，请确认选择的是已解压的扩展根目录。",
                unchecked((int)0x80070005) => "无法加载该扩展（路径或文件名不符合要求）。",
                unchecked((int)0x80070032) => "当前环境未启用浏览器扩展支持。",
                _ => $"操作失败：{ex.Message}"
            };
        }

        return ex.Message;
    }
}