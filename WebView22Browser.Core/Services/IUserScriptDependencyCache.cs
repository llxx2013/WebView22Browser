using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public interface IUserScriptDependencyCache
{
    Task<DependencyFetchResult> GetOrFetchAsync(string url, CancellationToken cancellationToken = default);
}
