using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class ExtensionPathValidatorTests
{
    [Fact]
    public void IsValidExtensionFolder_EmptyPath_ReturnsFalse()
    {
        var valid = ExtensionPathValidator.IsValidExtensionFolder(null, out var error);

        Assert.False(valid);
        Assert.NotNull(error);
    }

    [Fact]
    public void IsValidExtensionFolder_MissingManifest_ReturnsFalse()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ext-{Guid.NewGuid()}")).FullName;
        try
        {
            var valid = ExtensionPathValidator.IsValidExtensionFolder(dir, out var error);

            Assert.False(valid);
            Assert.Contains("manifest.json", error, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void IsValidExtensionFolder_WithManifest_ReturnsTrue()
    {
        var dir = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"ext-{Guid.NewGuid()}")).FullName;
        try
        {
            File.WriteAllText(
                Path.Combine(dir, ExtensionPathValidator.ManifestFileName),
                """{"name":"Test Extension","manifest_version":3,"version":"1.0"}""");

            var valid = ExtensionPathValidator.IsValidExtensionFolder(dir, out var error);

            Assert.True(valid);
            Assert.Null(error);
            Assert.Equal("Test Extension", ExtensionPathValidator.TryReadManifestName(dir));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }
}
