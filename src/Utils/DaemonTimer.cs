using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet;

public class DaemonTimer
{
    public delegate void TriggerFunc();

    public DaemonTimer(long interval, TriggerFunc onTrigger)
    {
        var time = TimeUtil.TickTime();
        new Thread(() =>
        {
            while (SuppressUtil.True)
            {
                onTrigger();
                var nextTime = time + interval;
                var sleepTime = Math.Max(0, nextTime - TimeUtil.TickTime());
                Thread.Sleep(TimeSpan.FromTicks(sleepTime));
                time = nextTime;
            }
        }) { IsBackground = true }.AntiGc().Start();
    }

    public static DaemonTimer FromNanos(double nanos, TriggerFunc onTrigger)
    {
        return new DaemonTimer((long)TimeUtil.NanoToTick(nanos), onTrigger);
    }

    public static DaemonTimer FromFrequency(double frequency, TriggerFunc onTrigger)
    {
        return new DaemonTimer((long)TimeUtil.NanoToTick(1e9 / frequency), onTrigger);
    }
}