using System.Text.RegularExpressions;

using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class UserScriptMetadataParser
{
    private static readonly Regex MetadataDirectiveRegex = new(
        @"^\s*//\s*@(\S+)(?:\s+(.*))?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> UnsupportedDirectives = new(StringComparer.OrdinalIgnoreCase)
    {
        "require", "resource", "antifeature", "icon", "version", "description",
        "author", "license", "namespace", "updateURL", "downloadURL", "supportURL", "homepage"
    };

    public static UserScriptParseResult Parse(string fileContent)
    {
        var result = new UserScriptParseResult();
        if (string.IsNullOrWhiteSpace(fileContent))
        {
            result.Warnings.Add("脚本文件为空。");
            return result;
        }

        var lines = fileContent.Replace("\r\n", "\n").Split('\n');
        var blockStart = FindLineIndex(lines, "==UserScript==");
        var blockEnd = FindLineIndex(lines, "==/UserScript==");

        if (blockStart < 0 || blockEnd < 0 || blockEnd <= blockStart)
        {
            result.Warnings.Add("未找到 // ==UserScript== 元数据块，已将整个文件作为脚本代码。");
            result.Code = fileContent.Trim();
            return result;
        }

        var grantNoneLocked = false;

        for (var i = blockStart + 1; i < blockEnd; i++)
        {
            var line = lines[i];
            var match = MetadataDirectiveRegex.Match(line);
            if (!match.Success)
                continue;

            var key = match.Groups[1].Value;
            var value = match.Groups[2].Value.Trim();

            if (UnsupportedDirectives.Contains(key))
            {
                result.Warnings.Add($"@{key} 不受支持，已忽略。");
                continue;
            }

            switch (key.ToLowerInvariant())
            {
                case "name":
                    result.Name = value;
                    break;
                case "match":
                case "include":
                    if (!string.IsNullOrWhiteSpace(value))
                        result.MatchPatterns = Append(result.MatchPatterns, value);
                    break;
                case "exclude":
                    if (!string.IsNullOrWhiteSpace(value))
                        result.ExcludePatterns = Append(result.ExcludePatterns, value);
                    break;
                case "run-at":
                    result.RunAt = ParseRunAt(value, result.Warnings);
                    break;
                case "noframes":
                    result.RunInTopFrameOnly = true;
                    break;
                case "grant":
                    if (string.Equals(value, "none", StringComparison.OrdinalIgnoreCase))
                    {
                        result.Grants = [];
                        grantNoneLocked = true;
                    }
                    else if (!grantNoneLocked)
                    {
                        if (!UserScriptGrantCatalog.KnownGrants.Contains(value))
                            result.Warnings.Add($"未知的 @grant「{value}」，已记录但阶段 1 可能不可用。");

                        result.Grants = Append(result.Grants, value);
                    }
                    break;
                case "connect":
                    if (!string.IsNullOrWhiteSpace(value))
                        result.ConnectPatterns = Append(result.ConnectPatterns, value);
                    break;
                default:
                    result.Warnings.Add($"@{key} 未识别，已忽略。");
                    break;
            }
        }

        if (result.MatchPatterns.Length == 0)
            result.Warnings.Add("元数据块中未包含 @match 或 @include。");

        var codeLines = lines.Skip(blockEnd + 1).ToList();
        while (codeLines.Count > 0 && string.IsNullOrWhiteSpace(codeLines[0]))
            codeLines.RemoveAt(0);

        result.Code = string.Join(Environment.NewLine, codeLines);
        return result;
    }

    private static int FindLineIndex(string[] lines, string marker)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Contains(marker, StringComparison.Ordinal))
                return i;
        }

        return -1;
    }

    private static string[] Append(string[] existing, string value) =>
        existing.Concat([value]).ToArray();

    private static UserScriptRunAt ParseRunAt(string value, List<string> warnings)
    {
        switch (value.ToLowerInvariant())
        {
            case "document-start":
            case "":
                return UserScriptRunAt.DocumentStart;
            case "document-end":
                return UserScriptRunAt.DocumentEnd;
            case "document-idle":
                return UserScriptRunAt.DocumentIdle;
            default:
                warnings.Add($"未知的 @run-at 值「{value}」，已使用 document-start。");
                return UserScriptRunAt.DocumentStart;
        }
    }
}