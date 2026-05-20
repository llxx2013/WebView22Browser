using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests.Fakes;

public sealed class FakeUserScriptDependencyResolver : IUserScriptDependencyResolver
{
    public Task<ResolvedScriptDependencies> ResolveAsync(
        UserScriptEntry entry,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new ResolvedScriptDependencies());

    public Task<IReadOnlyDictionary<Guid, ResolvedScriptDependencies>> ResolveAllAsync(
        IEnumerable<UserScriptEntry> entries,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<Guid, ResolvedScriptDependencies>>(
            new Dictionary<Guid, ResolvedScriptDependencies>());
}
