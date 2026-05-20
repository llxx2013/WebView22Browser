using System.IO;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

public sealed class UserScriptImportService
{
    private readonly IDialogService _dialogService;
    private readonly UserScriptExtensionConflictService _conflictService;

    public UserScriptImportService(
        IDialogService dialogService,
        UserScriptExtensionConflictService conflictService)
    {
        _dialogService = dialogService;
        _conflictService = conflictService;
    }

    public async Task<UserScriptEntry?> ImportFromFileAsync(string path)
    {
        string content;
        try
        {
            content = await File.ReadAllTextAsync(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _dialogService.ShowError($"无法读取文件：{ex.Message}", "导入用户脚本");
            return null;
        }

        var parsed = UserScriptMetadataParser.Parse(content);
        var entry = new UserScriptEntry
        {
            Name = string.IsNullOrWhiteSpace(parsed.Name) ? Path.GetFileNameWithoutExtension(path) : parsed.Name,
            MatchPatterns = parsed.MatchPatterns,
            ExcludePatterns = parsed.ExcludePatterns,
            Code = parsed.Code,
            RunAt = parsed.RunAt,
            RunInTopFrameOnly = parsed.RunInTopFrameOnly,
            Grants = parsed.Grants.ToArray(),
            ConnectPatterns = parsed.ConnectPatterns.ToArray(),
            Enabled = true,
            SourceFilePath = path
        };

        var messages = new List<string>();
        messages.AddRange(parsed.Warnings);

        var conflictCheck = _conflictService.CheckConflictsForScript(entry);
        if (!conflictCheck.IsAvailable)
            messages.Add(conflictCheck.UnavailableReason!);
        else if (conflictCheck.Conflicts.Count > 0)
            messages.Add(UserScriptExtensionConflictService.FormatConflictMessage(conflictCheck.Conflicts));

        if (messages.Count > 0)
        {
            var summary = string.Join(Environment.NewLine + Environment.NewLine, messages);
            if (!_dialogService.Confirm(
                    summary + Environment.NewLine + Environment.NewLine + "是否仍要导入？",
                    "导入用户脚本"))
                return null;
        }

        return entry;
    }
}
