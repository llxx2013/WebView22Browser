using System.Collections.ObjectModel;

namespace WebView22Browser.App.ViewModels;

public sealed class HistoryGroupViewModel
{
    public HistoryGroupViewModel(string label, IEnumerable<HistoryEntryViewModel> items)
    {
        Label = label;
        Items = new ObservableCollection<HistoryEntryViewModel>(items);
    }

    public string Label { get; }

    public ObservableCollection<HistoryEntryViewModel> Items { get; }
}
