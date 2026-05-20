using CommunityToolkit.Mvvm.ComponentModel;

using WebView22Browser.Core.Models;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.ViewModels;

public sealed partial class UserScriptItemViewModel : ObservableObject
{
    public UserScriptItemViewModel(UserScriptEntry entry)
    {
        Entry = entry;
    }

    public UserScriptEntry Entry { get; }

    public Guid Id => Entry.Id;

    public string Name => Entry.Name;

    public string MatchSummary =>
        Entry.MatchPatterns.Length == 0
            ? "(无匹配规则)"
            : Entry.MatchPatterns.Length == 1
                ? Entry.MatchPatterns[0]
                : $"{Entry.MatchPatterns[0]} (+{Entry.MatchPatterns.Length - 1})";

    public string RunAtSummary => Entry.RunAt switch
    {
        UserScriptRunAt.DocumentEnd => "document-end",
        UserScriptRunAt.DocumentIdle => "document-idle",
        _ => "document-start"
    };

    public string TopFrameLabel => Entry.RunInTopFrameOnly ? "仅顶层" : string.Empty;

    public bool IsEnabled => Entry.Enabled;

    [ObservableProperty]
    private string _dependencySummary = string.Empty;

    public void UpdateDependencyStatus(ResolvedScriptDependencies? resolved) =>
        DependencySummary = UserScriptDependencyStatus.FormatSummary(Entry, resolved, Entry.Enabled);

    public void Refresh()
    {
        OnPropertyChanged(string.Empty);
        UpdateDependencyStatus(null);
    }
}