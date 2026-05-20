using System.Net.Http;
using WebView22Browser.App.Services;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;
using WebView22Browser.Tests.Fakes;

namespace WebView22Browser.Tests;

internal static class TestServiceFactory
{
    public static UserScriptBridge CreateUserScriptBridge(IGmStorageStore gmStore) =>
        new(
            new FakeDialogService(),
            gmStore,
            new GmXhrMessageHandler(
                new GmXhrService(new HttpClient(new HttpClientHandler { UseCookies = false })),
                new JsonUserScriptStore(Path.Combine(Path.GetTempPath(), $"xhr-bridge-{Guid.NewGuid():N}.json"))),
            new WpfGmTabService(),
            new WpfGmClipboardService());
}
