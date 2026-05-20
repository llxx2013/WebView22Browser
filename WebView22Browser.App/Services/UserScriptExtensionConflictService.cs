using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

public sealed class UserScriptExtensionConflictService
{
    public const string ExtensionServiceNotReadyMessage =
        "扩展程序服务尚未就绪，无法检测与 Chromium 扩展的 URL 冲突。请先打开至少一个标签页并等待 WebView2 初始化完成。";

    private readonly BrowserExtensionService _extensionService;
    private readonly ExtensionManifestReader _manifestReader;

    public UserScriptExtensionConflictService(
        BrowserExtensionService extensionService,
        ExtensionManifestReader manifestReader)
    {
        _extensionService = extensionService;
        _manifestReader = manifestReader;
    }

    public bool IsConflictCheckAvailable => _extensionService.IsInitialized;

    public ExtensionConflictCheckResult CheckConflictsForPatterns(IEnumerable<string> userPatterns)
    {
        if (!_extensionService.IsInitialized)
            return ExtensionConflictCheckResult.Unavailable(ExtensionServiceNotReadyMessage);

        var extensions = GetEnabledExtensionInfos();
        var conflicts = UserScriptConflictDetector.FindConflicts(userPatterns, extensions);
        return ExtensionConflictCheckResult.Available(conflicts);
    }

    public ExtensionConflictCheckResult CheckConflictsForScript(UserScriptEntry entry) =>
        CheckConflictsForPatterns(entry.MatchPatterns);

    public IReadOnlyList<UserScriptConflict> GetConflictsForPatterns(IEnumerable<string> userPatterns) =>
        CheckConflictsForPatterns(userPatterns).Conflicts;

    public IReadOnlyList<UserScriptConflict> GetConflictsForScript(UserScriptEntry entry) =>
        CheckConflictsForScript(entry).Conflicts;

    public IReadOnlyList<ExtensionContentScriptInfo> GetEnabledExtensionInfos()
    {
        var list = new List<ExtensionContentScriptInfo>();

        if (!_extensionService.IsInitialized)
            return list;

        foreach (var item in _extensionService.Items)
        {
            if (!item.IsEnabled || string.IsNullOrWhiteSpace(item.SourcePath))
                continue;

            var info = _manifestReader.TryRead(item.SourcePath, item.Id, item.Name);
            if (info != null)
                list.Add(info);
        }

        return list;
    }

    public static string FormatConflictMessage(IReadOnlyList<UserScriptConflict> conflicts)
    {
        if (conflicts.Count == 0)
            return string.Empty;

        var lines = conflicts.Take(8).Select(c =>
            $"• 扩展「{c.ExtensionName}」({c.ExtensionPattern}) 与用户规则 ({c.UserPattern}) 可能同时作用于：{c.SampleUrl}");

        var body = string.Join(Environment.NewLine, lines);
        if (conflicts.Count > 8)
            body += $"{Environment.NewLine}…另有 {conflicts.Count - 8} 条重叠。";

        return "以下已启用的 Chromium 扩展 content script 可能与该用户脚本在相同 URL 上同时运行：" +
               Environment.NewLine + Environment.NewLine + body;
    }
}
