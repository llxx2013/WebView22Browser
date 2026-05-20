namespace WebView22Browser.Core.Services;

public sealed class TabNavigationHistory
{
    private readonly List<string> _entries = new();
    private readonly int _maxEntries;
    private int _currentIndex = -1;

    public TabNavigationHistory(int maxEntries = 50)
    {
        _maxEntries = Math.Max(1, maxEntries);
    }

    public int CurrentIndex => _currentIndex;

    public IReadOnlyList<string> Entries => _entries;

    public void RecordNavigation(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return;

        if (_currentIndex < 0)
        {
            _entries.Add(uri);
            _currentIndex = 0;
            return;
        }

        if (UriEquals(_entries[_currentIndex], uri))
            return;

        if (_currentIndex > 0 && UriEquals(_entries[_currentIndex - 1], uri))
        {
            _currentIndex--;
            return;
        }

        if (_currentIndex < _entries.Count - 1 && UriEquals(_entries[_currentIndex + 1], uri))
        {
            _currentIndex++;
            return;
        }

        if (_currentIndex < _entries.Count - 1)
            _entries.RemoveRange(_currentIndex + 1, _entries.Count - _currentIndex - 1);

        _entries.Add(uri);
        _currentIndex = _entries.Count - 1;
        TrimToMaxEntries();
    }

    public TabNavigationSnapshot GetSnapshot() =>
        new(_entries.ToList(), _currentIndex);

    public void RestoreSnapshot(IReadOnlyList<string> entries, int index)
    {
        _entries.Clear();
        if (entries.Count == 0)
        {
            _currentIndex = -1;
            return;
        }

        _entries.AddRange(entries);
        _currentIndex = Math.Clamp(index, 0, _entries.Count - 1);
    }

    public void Clear()
    {
        _entries.Clear();
        _currentIndex = -1;
    }

    private void TrimToMaxEntries()
    {
        while (_entries.Count > _maxEntries)
        {
            _entries.RemoveAt(0);
            _currentIndex--;
        }
    }

    private static bool UriEquals(string a, string b) =>
        string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}

public readonly record struct TabNavigationSnapshot(IReadOnlyList<string> Entries, int CurrentIndex);