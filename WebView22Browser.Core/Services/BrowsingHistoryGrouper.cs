using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public sealed record HistoryGroup(string Label, IReadOnlyList<BrowsingHistoryEntry> Entries);

public static class BrowsingHistoryGrouper
{
    public const string TodayLabel = "今天";
    public const string YesterdayLabel = "昨天";
    public const string LastSevenDaysLabel = "过去 7 天";
    public const string OlderLabel = "更早";

    public static IReadOnlyList<HistoryGroup> GroupByDate(
        IEnumerable<BrowsingHistoryEntry> entries,
        DateTime? referenceLocalTime = null)
    {
        var now = referenceLocalTime ?? DateTime.Now;
        var today = now.Date;
        var yesterday = today.AddDays(-1);
        var sevenDaysAgo = today.AddDays(-7);

        var todayItems = new List<BrowsingHistoryEntry>();
        var yesterdayItems = new List<BrowsingHistoryEntry>();
        var lastSevenItems = new List<BrowsingHistoryEntry>();
        var olderItems = new List<BrowsingHistoryEntry>();

        foreach (var entry in entries)
        {
            var localDate = entry.VisitedAtUtc.ToLocalTime().Date;

            if (localDate == today)
                todayItems.Add(entry);
            else if (localDate == yesterday)
                yesterdayItems.Add(entry);
            else if (localDate >= sevenDaysAgo)
                lastSevenItems.Add(entry);
            else
                olderItems.Add(entry);
        }

        var groups = new List<HistoryGroup>();
        if (todayItems.Count > 0)
            groups.Add(new HistoryGroup(TodayLabel, todayItems));
        if (yesterdayItems.Count > 0)
            groups.Add(new HistoryGroup(YesterdayLabel, yesterdayItems));
        if (lastSevenItems.Count > 0)
            groups.Add(new HistoryGroup(LastSevenDaysLabel, lastSevenItems));
        if (olderItems.Count > 0)
            groups.Add(new HistoryGroup(OlderLabel, olderItems));

        return groups;
    }
}
