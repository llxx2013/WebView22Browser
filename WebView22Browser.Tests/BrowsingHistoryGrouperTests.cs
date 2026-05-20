using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class BrowsingHistoryGrouperTests
{
    [Fact]
    public void GroupByDate_AssignsCorrectLabels()
    {
        var reference = new DateTime(2026, 5, 19, 12, 0, 0, DateTimeKind.Local);
        var todayUtc = reference.ToUniversalTime();
        var yesterdayUtc = reference.AddDays(-1).ToUniversalTime();
        var threeDaysAgoUtc = reference.AddDays(-3).ToUniversalTime();
        var tenDaysAgoUtc = reference.AddDays(-10).ToUniversalTime();

        var entries = new[]
        {
            new BrowsingHistoryEntry { Url = "https://today.com", VisitedAtUtc = todayUtc },
            new BrowsingHistoryEntry { Url = "https://yesterday.com", VisitedAtUtc = yesterdayUtc },
            new BrowsingHistoryEntry { Url = "https://week.com", VisitedAtUtc = threeDaysAgoUtc },
            new BrowsingHistoryEntry { Url = "https://old.com", VisitedAtUtc = tenDaysAgoUtc }
        };

        var groups = BrowsingHistoryGrouper.GroupByDate(entries, reference);

        Assert.Equal(4, groups.Count);
        Assert.Equal(BrowsingHistoryGrouper.TodayLabel, groups[0].Label);
        Assert.Equal(BrowsingHistoryGrouper.YesterdayLabel, groups[1].Label);
        Assert.Equal(BrowsingHistoryGrouper.LastSevenDaysLabel, groups[2].Label);
        Assert.Equal(BrowsingHistoryGrouper.OlderLabel, groups[3].Label);
    }
}