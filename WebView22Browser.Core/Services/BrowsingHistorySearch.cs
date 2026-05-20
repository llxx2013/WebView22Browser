using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public static class BrowsingHistorySearch
{
    public static IEnumerable<BrowsingHistoryEntry> Filter(
        IEnumerable<BrowsingHistoryEntry> entries,
        string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return entries;

        var term = query.Trim();
        return entries.Where(e =>
            e.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            e.Url.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}
