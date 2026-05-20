using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public interface IUserScriptDependencyResolver
{
    Task<ResolvedScriptDependencies> ResolveAsync(UserScriptEntry entry, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, ResolvedScriptDependencies>> ResolveAllAsync(
        IEnumerable<UserScriptEntry> entries,
        CancellationToken cancellationToken = default);
}