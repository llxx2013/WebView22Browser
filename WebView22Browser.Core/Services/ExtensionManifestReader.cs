using System.Text.Json;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public sealed class ExtensionManifestReader
{
    public ExtensionContentScriptInfo? TryRead(string extensionFolderPath, string extensionId, string? extensionName = null)
    {
        if (!ExtensionPathValidator.IsValidExtensionFolder(extensionFolderPath, out _))
            return null;

        try
        {
            var manifestPath = Path.Combine(extensionFolderPath, ExtensionPathValidator.ManifestFileName);
            using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = doc.RootElement;

            var name = extensionName ?? ExtensionPathValidator.TryReadManifestName(extensionFolderPath) ?? extensionId;
            var patterns = new List<string>();

            if (root.TryGetProperty("content_scripts", out var contentScripts) &&
                contentScripts.ValueKind == JsonValueKind.Array)
            {
                foreach (var script in contentScripts.EnumerateArray())
                {
                    if (!script.TryGetProperty("matches", out var matches) ||
                        matches.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var match in matches.EnumerateArray())
                    {
                        if (match.ValueKind == JsonValueKind.String)
                        {
                            var pattern = match.GetString();
                            if (!string.IsNullOrWhiteSpace(pattern))
                                patterns.Add(pattern);
                        }
                    }
                }
            }

            if (patterns.Count == 0)
                return null;

            return new ExtensionContentScriptInfo
            {
                ExtensionName = name,
                ExtensionId = extensionId,
                MatchPatterns = patterns.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }
}
