using System.Text.Json;

namespace WebView22Browser.Core.Services;

public static class ExtensionPathValidator
{
    public const string ManifestFileName = "manifest.json";

    public static bool IsValidExtensionFolder(string? folderPath, out string? errorMessage)
    {
        errorMessage = null;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            errorMessage = "扩展目录路径不能为空。";
            return false;
        }

        if (!Directory.Exists(folderPath))
        {
            errorMessage = "扩展目录不存在。";
            return false;
        }

        var manifestPath = Path.Combine(folderPath, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            errorMessage = $"未找到 {ManifestFileName}，请选择包含有效扩展清单的文件夹。";
            return false;
        }

        return true;
    }

    public static string? TryReadManifestName(string folderPath)
    {
        if (!IsValidExtensionFolder(folderPath, out _))
            return null;

        try
        {
            var manifestPath = Path.Combine(folderPath, ManifestFileName);
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            if (doc.RootElement.TryGetProperty("name", out var nameElement))
            {
                return nameElement.ValueKind switch
                {
                    JsonValueKind.String => nameElement.GetString(),
                    _ => null
                };
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}