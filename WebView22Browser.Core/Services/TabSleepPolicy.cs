namespace WebView22Browser.Core.Services;

public enum SystemPressureLevel
{
    Normal,
    Elevated,
    High
}

public enum TabSleepAction
{
    None,
    ReduceMemory,
    Suspend,
    Destroy
}

public readonly record struct TabSleepCandidate(
    bool IsEnabled,
    TimeSpan Timeout,
    bool IsWebViewReady,
    bool IsSleeping,
    bool IsSelected,
    bool IsLoading,
    bool IsPlayingAudio,
    bool HasActiveDownloads,
    TimeSpan IdleDuration,
    SystemPressureLevel Pressure = SystemPressureLevel.Normal,
    bool IsMemoryReduced = false,
    bool IsLightSuspended = false);

public static class TabSleepPolicy
{
    public static TabSleepAction Decide(TabSleepCandidate candidate)
    {
        if (!candidate.IsEnabled
            || candidate.Timeout <= TimeSpan.Zero
            || !candidate.IsWebViewReady
            || candidate.IsSleeping
            || candidate.IsSelected
            || candidate.IsLoading
            || candidate.IsPlayingAudio
            || candidate.HasActiveDownloads)
        {
            return TabSleepAction.None;
        }

        var effectiveTimeout = GetEffectiveTimeout(candidate.Timeout, candidate.Pressure);
        var idle = candidate.IdleDuration;

        if (idle < effectiveTimeout * 0.5)
            return TabSleepAction.None;

        if (ShouldDestroy(idle, effectiveTimeout, candidate.Pressure))
            return TabSleepAction.Destroy;

        if (idle >= effectiveTimeout)
        {
            if (candidate.IsLightSuspended)
                return TabSleepAction.None;

            return TabSleepAction.Suspend;
        }

        if (candidate.IsMemoryReduced)
            return TabSleepAction.None;

        return TabSleepAction.ReduceMemory;
    }

    public static bool CanSleep(TabSleepCandidate candidate) =>
        Decide(candidate) != TabSleepAction.None;

    private static TimeSpan GetEffectiveTimeout(TimeSpan timeout, SystemPressureLevel pressure) =>
        pressure switch
        {
            SystemPressureLevel.High => TimeSpan.FromTicks((long)(timeout.Ticks * 0.2)),
            SystemPressureLevel.Elevated => TimeSpan.FromTicks((long)(timeout.Ticks * 0.5)),
            _ => timeout
        };

    private static bool ShouldDestroy(TimeSpan idle, TimeSpan effectiveTimeout, SystemPressureLevel pressure)
    {
        if (idle >= effectiveTimeout * 2)
            return true;

        return pressure == SystemPressureLevel.High && idle >= effectiveTimeout * 1.5;
    }
}