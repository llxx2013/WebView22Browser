using System.Net;
using System.Net.Http;

using WebView22Browser.App.Services;

namespace WebView22Browser.Tests;

public class UserScriptDependencyCacheTests
{
    [Fact]
    public async Task GetOrFetchAsync_CachesSuccessfulResponse()
    {
        const string body = "window.mockLib = 1;";
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body)
        });
        var cacheDir = Path.Combine(Path.GetTempPath(), $"script-deps-{Guid.NewGuid():N}");
        var cache = new UserScriptDependencyCache(new HttpClient(handler), cacheDir);

        var first = await cache.GetOrFetchAsync("https://cdn.example/lib.js");
        var second = await cache.GetOrFetchAsync("https://cdn.example/lib.js");

        Assert.True(first.Success);
        Assert.Equal(body, first.Content);
        Assert.True(second.Success);
        Assert.Equal(body, second.Content);
        Assert.Equal(1, handler.SendCount);
        Assert.True(Directory.EnumerateFiles(cacheDir).Any());
    }

    [Fact]
    public async Task GetOrFetchAsync_RejectsNonHttpScheme()
    {
        var cache = new UserScriptDependencyCache(
            new HttpClient(new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK))),
            Path.Combine(Path.GetTempPath(), $"script-deps-{Guid.NewGuid():N}"));

        var result = await cache.GetOrFetchAsync("file:///local/script.js");

        Assert.False(result.Success);
        Assert.Contains("http/https", result.Error);
    }

    [Fact]
    public async Task GetOrFetchAsync_OversizedBody_Fails()
    {
        var oversized = new string('x', UserScriptDependencyCache.MaxFileBytes + 1);
        var handler = new StubHttpHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(oversized)
        });
        var cache = new UserScriptDependencyCache(
            new HttpClient(handler),
            Path.Combine(Path.GetTempPath(), $"script-deps-{Guid.NewGuid():N}"));

        var result = await cache.GetOrFetchAsync("https://cdn.example/huge.js");

        Assert.False(result.Success);
        Assert.NotNull(result.Error);
    }

    private sealed class StubHttpHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _factory;

        public StubHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> factory) =>
            _factory = factory;

        public int SendCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.FromResult(_factory(request));
        }
    }
}