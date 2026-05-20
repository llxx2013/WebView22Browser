using WebView22Browser.Core;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.App.Services;

public sealed class BrowsingHistoryService : IBrowsingHistoryService
{
    private readonly IBrowsingHistoryStore _store;
    private readonly BrowserOptions _options;

    public BrowsingHistoryService(IBrowsingHistoryStore store, BrowserOptions options)
    {
        _store = store;
        _options = options;
    }

    public event Action? HistoryChanged;

    public async Task RecordVisitAsync(string url, string? title, CancellationToken cancellationToken = default)
    {
        if (!BrowsingHistoryPolicy.ShouldRecord(url))
            return;

        _store.AddAndTrimToMax(new BrowsingHistoryEntry
        {
            Url = url,
            Title = BrowsingHistoryTitleFormatter.Format(url, title),
            VisitedAtUtc = DateTime.UtcNow
        }, _options.BrowsingHistoryMaxEntries);
        await _store.SaveAsync(cancellationToken);
        HistoryChanged?.Invoke();
    }
}
