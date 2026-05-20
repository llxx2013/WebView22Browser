using System.Text.Json;

namespace WebView22Browser.Core.Stores;

public interface IGmStorageStore
{
    Task<IReadOnlyDictionary<string, JsonElement>> LoadAsync(Guid scriptId, CancellationToken cancellationToken = default);

    Task SetValueAsync(Guid scriptId, string key, JsonElement value, CancellationToken cancellationToken = default);

    Task DeleteValueAsync(Guid scriptId, string key, CancellationToken cancellationToken = default);

    Task DeleteScriptAsync(Guid scriptId, CancellationToken cancellationToken = default);
}