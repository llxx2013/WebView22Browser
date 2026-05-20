using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonUserSettingsStoreLoadTests
{
    [Fact]
    public async Task LoadAsync_InvalidJson_DoesNotThrowAndClearsCurrent()
    {
        var path = Path.Combine(Path.GetTempPath(), $"user-settings-bad-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(path, "{ not valid json }");

        var store = new JsonUserSettingsStore(path);
        await store.LoadAsync();

        Assert.Null(store.Current);
    }
}
