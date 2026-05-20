using CommunityToolkit.Mvvm.ComponentModel;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.ViewModels;

public partial class HistoryEntryViewModel : ObservableObject
{
    public HistoryEntryViewModel(BrowsingHistoryEntry entry)
    {
        Id = entry.Id;
        Url = entry.Url;
        Title = BrowsingHistoryTitleFormatter.Format(entry.Url, entry.Title);
        VisitedAtLocal = entry.VisitedAtUtc.ToLocalTime();
        TimeText = VisitedAtLocal.ToString("HH:mm");
    }

    public Guid Id { get; }

    public string Url { get; }

    public string Title { get; }

    public DateTime VisitedAtLocal { get; }

    public string TimeText { get; }
}