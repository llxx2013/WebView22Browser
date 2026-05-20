using System.Text.Json;

using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonFileStoreBaseTests
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    [Fact]
    public async Task WriteAtomicAsync_LeavesNoTempFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"atomic-{Guid.NewGuid():N}.json");
        try
        {
            await JsonFileStoreBase.WriteAtomicAsync(path, new SampleDto { Name = "ok" }, JsonOptions);

            Assert.True(File.Exists(path));
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAtomicAsync_OnFailure_PreservesOriginalFile()
    {
        var path = Path.Combine(Path.GetTempPath(), $"atomic-{Guid.NewGuid():N}.json");
        var invalidDir = Path.Combine(path, "nested", "file.json");
        try
        {
            await JsonFileStoreBase.WriteAtomicAsync(path, new SampleDto { Name = "original" }, JsonOptions);

            // Parent path is an existing file (not a directory); OS throws IOException on
            // Windows and may throw DirectoryNotFoundException on Linux.
            await Assert.ThrowsAnyAsync<Exception>(() =>
                JsonFileStoreBase.WriteAtomicAsync(invalidDir, new SampleDto { Name = "broken" }, JsonOptions));

            await using (var stream = File.OpenRead(path))
            {
                var reloaded = await JsonSerializer.DeserializeAsync<SampleDto>(stream);
                Assert.Equal("original", reloaded?.Name);
            }
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    private sealed class SampleDto
    {
        public string Name { get; set; } = string.Empty;
    }
}