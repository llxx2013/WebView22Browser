using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Stores;

public interface IUserScriptStore
{
    IReadOnlyList<UserScriptEntry> Items { get; }

    Task LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);

    UserScriptEntry Add(UserScriptEntry entry);

    void Update(UserScriptEntry entry);

    void Remove(Guid id);

    UserScriptEntry? FindById(Guid id);
}