using WebView22Browser.App.ViewModels;
using WebView22Browser.Core.Models;

namespace WebView22Browser.Tests;

public class DownloadItemViewModelTests
{
    [Fact]
    public void SetState_Completed_EnablesShowInFolderWhenFileExists()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dl-vm-{Guid.NewGuid()}.txt");
        File.WriteAllText(path, "x");

        try
        {
            var item = new DownloadItemViewModel
            {
                FullPath = path,
                State = DownloadState.InProgress
            };

            Assert.False(item.CanShowInFolder);

            item.SetState(DownloadState.Completed);

            Assert.True(item.CanShowInFolder);
            Assert.True(item.CanOpen);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Fact]
    public void FromHistoryEntry_InProgressBecomesInterrupted()
    {
        var entry = new DownloadHistoryEntry { State = DownloadState.InProgress };
        var item = DownloadItemViewModel.FromHistoryEntry(entry);
        Assert.Equal(DownloadState.Interrupted, item.State);
    }
}
