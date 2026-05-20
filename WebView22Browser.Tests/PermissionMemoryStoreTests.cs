using Microsoft.Web.WebView2.Core;

using WebView22Browser.App.Services;
using WebView22Browser.Core;

namespace WebView22Browser.Tests;

public class PermissionMemoryStoreTests
{
    [Fact]
    public void TryGet_NotFound_ReturnsFalse()
    {
        var store = CreateStore();
        var found = store.TryGet("https://example.com", CoreWebView2PermissionKind.Geolocation, out _);
        Assert.False(found);
    }

    [Fact]
    public async Task Set_Then_TryGet_ReturnsStoredState()
    {
        var store = CreateStore(TimeSpan.FromMilliseconds(50));
        await store.SetAsync("https://example.com", CoreWebView2PermissionKind.Geolocation, CoreWebView2PermissionState.Allow);

        Assert.True(store.TryGet("https://example.com", CoreWebView2PermissionKind.Geolocation, out var state));
        Assert.Equal(CoreWebView2PermissionState.Allow, state);
    }

    [Fact]
    public async Task Load_CorruptJson_StartsEmpty()
    {
        var root = Path.Combine(Path.GetTempPath(), "wv2-perm-" + Guid.NewGuid());
        var permDir = Path.Combine(root, "WebView22Browser");
        Directory.CreateDirectory(permDir);
        await File.WriteAllTextAsync(Path.Combine(permDir, "permissions.json"), "{ invalid");

        var store = new PermissionMemoryStore(new BrowserOptions { UserDataRoot = root }, TimeSpan.FromMilliseconds(50));
        await store.LoadAsync();

        Assert.False(store.TryGet("https://x.com", CoreWebView2PermissionKind.Camera, out _));

        Directory.Delete(root, recursive: true);
    }

    [Fact]
    public async Task DisposeAsync_WhileFlushScheduled_DoesNotFault()
    {
        var store = CreateStore(TimeSpan.FromMilliseconds(500));
        await store.SetAsync("https://example.com", CoreWebView2PermissionKind.Geolocation, CoreWebView2PermissionState.Allow);

        await store.DisposeAsync();

        await Task.Delay(100);
    }

    [Fact]
    public async Task SetAsync_MultipleCalls_BatchesToSingleFlush()
    {
        var store = CreateStore(TimeSpan.FromMilliseconds(100));

        await store.SetAsync("https://a.com", CoreWebView2PermissionKind.Camera, CoreWebView2PermissionState.Allow);
        await store.SetAsync("https://b.com", CoreWebView2PermissionKind.Microphone, CoreWebView2PermissionState.Deny);
        await store.SetAsync("https://c.com", CoreWebView2PermissionKind.Notifications, CoreWebView2PermissionState.Allow);

        await Task.Delay(250);

        Assert.Equal(1, store.FlushCount);
    }

    private static PermissionMemoryStore CreateStore(TimeSpan? flushDelay = null)
    {
        var root = Path.Combine(Path.GetTempPath(), "wv2-perm-" + Guid.NewGuid());
        return new PermissionMemoryStore(new BrowserOptions { UserDataRoot = root }, flushDelay);
    }
}