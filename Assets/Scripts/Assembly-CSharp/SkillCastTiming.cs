using System;
using System.Diagnostics;

public static class SkillCastTiming
{
    // Shared by both tabs, independent of Unity timeScale and wall-clock corrections.
    public static Func<long> NowMilliseconds = MonotonicMilliseconds;

    private static long MonotonicMilliseconds()
    {
        return (long)(Stopwatch.GetTimestamp() * (1000.0 / Stopwatch.Frequency));
    }
}
