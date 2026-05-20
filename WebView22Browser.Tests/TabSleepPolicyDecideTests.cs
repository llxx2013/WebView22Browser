using WebView22Browser.Core.Services;

namespace WebView22Browser.Tests;

public class TabSleepPolicyDecideTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(5);

    private static TabSleepCandidate Candidate(
        TimeSpan? idleDuration = null,
        SystemPressureLevel pressure = SystemPressureLevel.Normal,
        bool isMemoryReduced = false,
        bool isLightSuspended = false,
        bool isSelected = false) =>
        new(
            IsEnabled: true,
            Timeout: Timeout,
            IsWebViewReady: true,
            IsSleeping: false,
            IsSelected: isSelected,
            IsLoading: false,
            IsPlayingAudio: false,
            HasActiveDownloads: false,
            IdleDuration: idleDuration ?? Timeout,
            Pressure: pressure,
            IsMemoryReduced: isMemoryReduced,
            IsLightSuspended: isLightSuspended);

    [Fact]
    public void Decide_WhenIdleBelowHalfTimeout_ReturnsNone()
    {
        Assert.Equal(
            TabSleepAction.None,
            TabSleepPolicy.Decide(Candidate(idleDuration: Timeout * 0.4)));
    }

    [Fact]
    public void Decide_WhenIdleAtHalfTimeout_ReturnsReduceMemory()
    {
        Assert.Equal(
            TabSleepAction.ReduceMemory,
            TabSleepPolicy.Decide(Candidate(idleDuration: Timeout * 0.5)));
    }

    [Fact]
    public void Decide_WhenIdleAtFullTimeout_ReturnsSuspend()
    {
        Assert.Equal(
            TabSleepAction.Suspend,
            TabSleepPolicy.Decide(Candidate(idleDuration: Timeout)));
    }

    [Fact]
    public void Decide_WhenIdleAtDoubleTimeout_ReturnsDestroy()
    {
        Assert.Equal(
            TabSleepAction.Destroy,
            TabSleepPolicy.Decide(Candidate(idleDuration: Timeout * 2)));
    }

    [Fact]
    public void Decide_WhenHighPressureAndIdleAtOnePointFiveTimeout_ReturnsDestroy()
    {
        Assert.Equal(
            TabSleepAction.Destroy,
            TabSleepPolicy.Decide(Candidate(
                idleDuration: Timeout * 1.5,
                pressure: SystemPressureLevel.High)));
    }

    [Fact]
    public void Decide_WhenHighPressure_ShortensEffectiveTimeout()
    {
        var effective = Timeout * 0.2;
        Assert.Equal(
            TabSleepAction.Suspend,
            TabSleepPolicy.Decide(Candidate(
                idleDuration: effective,
                pressure: SystemPressureLevel.High)));
    }

    [Fact]
    public void Decide_WhenAlreadyMemoryReduced_ReturnsNoneUntilSuspendThreshold()
    {
        Assert.Equal(
            TabSleepAction.None,
            TabSleepPolicy.Decide(Candidate(
                idleDuration: Timeout * 0.75,
                isMemoryReduced: true)));
    }

    [Fact]
    public void Decide_WhenAlreadyLightSuspended_ReturnsNoneUntilDestroy()
    {
        Assert.Equal(
            TabSleepAction.None,
            TabSleepPolicy.Decide(Candidate(
                idleDuration: Timeout,
                isLightSuspended: true)));
    }

    [Fact]
    public void Decide_WhenSelected_ReturnsNone()
    {
        Assert.Equal(
            TabSleepAction.None,
            TabSleepPolicy.Decide(Candidate(isSelected: true)));
    }
}
