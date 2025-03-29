using System.Diagnostics;

namespace YqlossKeyViewerDotNet.Utils;

public static class TimeUtil
{
    private static Stopwatch Stopwatch { get; } = new Stopwatch().Also(it => it.Start());
    public static long Frequency { get; } = Stopwatch.Frequency;

    public static long TickTime()
    {
        return Stopwatch.GetTimestamp();
    }

    public static double TickToNano(double ticks)
    {
        return ticks * 1e9 / Frequency;
    }

    public static double NanoToTick(double nanos)
    {
        return nanos * Frequency / 1e9;
    }
}