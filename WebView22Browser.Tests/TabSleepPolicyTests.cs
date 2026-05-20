using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class TabSleepPolicyTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private static TabSleepCandidate DefaultCandidate(
        bool isEnabled = true,
        TimeSpan? timeout = null,
        bool isWebViewReady = true,
        bool isSleeping = false,
        bool isSelected = false,
        bool isLoading = false,
        bool isPlayingAudio = false,
        bool hasActiveDownloads = false,
        TimeSpan? idleDuration = null) =>
        new(
            isEnabled,
            timeout ?? Timeout,
            isWebViewReady,
            isSleeping,
            isSelected,
            isLoading,
            isPlayingAudio,
            hasActiveDownloads,
            idleDuration ?? Timeout);

    [Fact]
    public void CanSleep_WhenIdleBackgroundTab_ReturnsTrue()
    {
        Assert.True(TabSleepPolicy.CanSleep(DefaultCandidate()));
    }

    [Fact]
    public void CanSleep_WhenSelected_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(isSelected: true)));
    }

    [Fact]
    public void CanSleep_WhenLoading_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(isLoading: true)));
    }

    [Fact]
    public void CanSleep_WhenAlreadySleeping_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(isSleeping: true)));
    }

    [Fact]
    public void CanSleep_WhenNotReady_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(isWebViewReady: false)));
    }

    [Fact]
    public void CanSleep_WhenIdleBelowHalfTimeout_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(
            DefaultCandidate(idleDuration: Timeout * 0.4)));
    }

    [Fact]
    public void CanSleep_WhenDisabled_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(isEnabled: false)));
    }

    [Fact]
    public void CanSleep_WhenTimeoutZero_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(
            DefaultCandidate(timeout: TimeSpan.Zero, isEnabled: true)));
    }

    [Fact]
    public void CanSleep_WhenPlayingAudio_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(isPlayingAudio: true)));
    }

    [Fact]
    public void CanSleep_WhenHasActiveDownloads_ReturnsFalse()
    {
        Assert.False(TabSleepPolicy.CanSleep(DefaultCandidate(hasActiveDownloads: true)));
    }
}