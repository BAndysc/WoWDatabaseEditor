using System.Diagnostics;

namespace TheEngine.Utils;

public readonly struct CheapStopWatch(long start)
{
    public static CheapStopWatch StartNew()
    {
        return new CheapStopWatch(Stopwatch.GetTimestamp());
    }

    public TimeSpan Elapsed => Stopwatch.GetElapsedTime(start);
}