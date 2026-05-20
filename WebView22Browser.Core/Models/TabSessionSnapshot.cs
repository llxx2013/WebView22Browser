namespace WebView22Browser.Core.Models;

public sealed class TabSessionFile
{
    public int Version { get; set; } = 1;

    public Guid? SelectedTabId { get; set; }

    public List<TabSessionSnapshot> Tabs { get; set; } = [];
}

public sealed class TabSessionSnapshot
{
    public Guid TabId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public List<string> History { get; set; } = [];

    public int CurrentIndex { get; set; } = -1;

    public DateTime LastActiveUtc { get; set; }
}
