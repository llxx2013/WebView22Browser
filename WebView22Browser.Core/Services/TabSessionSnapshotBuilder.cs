using WebView22Browser.Core.Models;

namespace WebView22Browser.Core.Services;

public readonly record struct TabSessionSource(
    Guid TabId,
    string Title,
    string Address,
    IReadOnlyList<string>? HistoryEntries,
    int HistoryIndex,
    DateTime LastActiveUtc);

public static class TabSessionSnapshotBuilder
{
    public static TabSessionFile Build(
        IReadOnlyList<TabSessionSource> tabs,
        Guid? selectedTabId,
        int maxHistoryEntries)
    {
        var file = new TabSessionFile
        {
            Version = 1,
            SelectedTabId = selectedTabId
        };

        foreach (var tab in tabs)
        {
            var history = tab.HistoryEntries?.ToList() ?? [];
            if (history.Count == 0 && !string.IsNullOrWhiteSpace(tab.Address))
            {
                history.Add(tab.Address);
            }

            var index = tab.HistoryIndex;
            TrimHistory(history, maxHistoryEntries, ref index);
            if (history.Count == 0)
                index = -1;

            file.Tabs.Add(new TabSessionSnapshot
            {
                TabId = tab.TabId,
                Title = tab.Title,
                Address = tab.Address,
                History = history,
                CurrentIndex = index,
                LastActiveUtc = tab.LastActiveUtc
            });
        }

        return file;
    }

    private static void TrimHistory(List<string> history, int maxEntries, ref int index)
    {
        var max = Math.Max(1, maxEntries);
        while (history.Count > max)
        {
            history.RemoveAt(0);
            index--;
        }

        if (history.Count > 0)
            index = Math.Clamp(index, 0, history.Count - 1);
    }
}
