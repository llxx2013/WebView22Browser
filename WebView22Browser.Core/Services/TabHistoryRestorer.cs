namespace WebView22Browser.Core.Services;

public enum TabHistoryRestoreActionKind
{
    Navigate,
    GoBack
}

public readonly record struct TabHistoryRestoreStep(TabHistoryRestoreActionKind Kind, string? Uri = null);

public static class TabHistoryRestorer
{
    public static IReadOnlyList<TabHistoryRestoreStep> BuildSteps(IReadOnlyList<string> entries, int targetIndex)
    {
        if (entries.Count == 0)
            return Array.Empty<TabHistoryRestoreStep>();

        var clampedIndex = Math.Clamp(targetIndex, 0, entries.Count - 1);
        var steps = new List<TabHistoryRestoreStep>(entries.Count + entries.Count);

        foreach (var entry in entries)
            steps.Add(new TabHistoryRestoreStep(TabHistoryRestoreActionKind.Navigate, entry));

        var goBackCount = entries.Count - 1 - clampedIndex;
        for (var i = 0; i < goBackCount; i++)
            steps.Add(new TabHistoryRestoreStep(TabHistoryRestoreActionKind.GoBack));

        return steps;
    }
}
