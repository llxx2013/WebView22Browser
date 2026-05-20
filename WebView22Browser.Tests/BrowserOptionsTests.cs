using WebView22Browser.Core;

namespace WebView22Browser.Tests;

public class BrowserOptionsTests
{
    [Fact]
    public void GetUserDataFolder_CustomRoot_UsesCustomPath()
    {
        var root = Path.Combine(Path.GetTempPath(), "wv2-test-" + Guid.NewGuid());
        var options = new BrowserOptions { UserDataRoot = root };

        var folder = options.GetUserDataFolder();

        Assert.StartsWith(root, folder);
        Assert.EndsWith(Path.Combine("WebView22Browser", "UserData", "Profile"), folder);
    }

    [Fact]
    public void GetDefaultFavoritesPath_CustomPath_ReturnsOverride()
    {
        var path = Path.Combine(Path.GetTempPath(), "favorites-custom.json");
        var options = new BrowserOptions { FavoritesFilePath = path };

        Assert.Equal(path, options.GetDefaultFavoritesPath());
    }
}