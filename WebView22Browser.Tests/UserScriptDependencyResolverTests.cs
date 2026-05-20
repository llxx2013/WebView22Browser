using WebView22Browser.App.Services;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class UserScriptDependencyResolverTests
{
    [Fact]
    public async Task ResolveAsync_MissingRequire_AddsWarning()
    {
        var cache = new InMemoryDependencyCache();
        var resolver = new UserScriptDependencyResolver(cache);
        var entry = new UserScriptEntry
        {
            RequireUrls = ["https://cdn.example/missing.js"]
        };

        var result = await resolver.ResolveAsync(entry);

        Assert.Empty(result.RequireScripts);
        Assert.Contains(result.Warnings, w => w.Contains("@require"));
    }

    [Fact]
    public async Task ResolveAsync_FetchesRequireAndResourceInOrder()
    {
        var cache = new InMemoryDependencyCache();
        cache.SetContent("https://cdn.example/a.js", "var a = 1;");
        cache.SetContent("https://cdn.example/style.css", "body { }");
        var resolver = new UserScriptDependencyResolver(cache);
        var entry = new UserScriptEntry
        {
            RequireUrls = ["https://cdn.example/a.js"],
            Resources = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["theme"] = "https://cdn.example/style.css"
            }
        };

        var result = await resolver.ResolveAsync(entry);

        Assert.Single(result.RequireScripts);
        Assert.Equal("var a = 1;", result.RequireScripts[0]);
        Assert.Equal("body { }", result.ResourceTexts["theme"]);
        Assert.Empty(result.Warnings);
    }

    private sealed class InMemoryDependencyCache : IUserScriptDependencyCache
    {
        private readonly Dictionary<string, string> _content = new(StringComparer.Ordinal);

        public void SetContent(string url, string content) => _content[url] = content;

        public Task<DependencyFetchResult> GetOrFetchAsync(string url, CancellationToken cancellationToken = default) =>
            _content.TryGetValue(url, out var text)
                ? Task.FromResult(new DependencyFetchResult { Success = true, Content = text })
                : Task.FromResult(new DependencyFetchResult { Success = false, Error = "not found" });
    }
}
