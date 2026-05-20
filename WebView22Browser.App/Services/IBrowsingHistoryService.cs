namespace WebView22Browser.App.Services;

public interface IBrowsingHistoryService
{
    event Action? HistoryChanged;

    Task RecordVisitAsync(string url, string? title, CancellationToken cancellationToken = default);

    void NotifyHistoryChanged();
}