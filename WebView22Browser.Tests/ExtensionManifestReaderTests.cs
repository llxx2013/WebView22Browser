using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class ExtensionManifestReaderTests
{
    [Fact]
    public void TryRead_ExtractsContentScriptMatches()
    {
        var dir = Path.Combine(Path.GetTempPath(), $"ext-manifest-{Guid.NewGuid()}");
        Directory.CreateDirectory(dir);
        try
        {
            File.WriteAllText(Path.Combine(dir, "manifest.json"), """
                {
                  "name": "Test Extension",
                  "manifest_version": 3,
                  "content_scripts": [
                    {
                      "matches": ["*://*.bing.com/*", "https://example.com/*"],
                      "js": ["content.js"]
                    }
                  ]
                }
                """);

            var reader = new ExtensionManifestReader();
            var info = reader.TryRead(dir, "ext-id-1");

            Assert.NotNull(info);
            Assert.Equal("Test Extension", info!.ExtensionName);
            Assert.Equal(2, info.MatchPatterns.Length);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void TryRead_InvalidFolder_ReturnsNull()
    {
        var reader = new ExtensionManifestReader();
        Assert.Null(reader.TryRead(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), "id"));
    }
}
