using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface IExtensionSourceStore
{
    IReadOnlyList<ExtensionSourceEntry> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    ExtensionSourceEntry Add(string folderPath, string? lastKnownExtensionId = null);

    void Remove(Guid id);

    void RemoveByExtensionId(string extensionId);

    ExtensionSourceEntry? FindByFolderPath(string folderPath);

    ExtensionSourceEntry? FindByExtensionId(string extensionId);
}
