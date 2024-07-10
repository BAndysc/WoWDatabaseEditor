using System;
using System.Text;

namespace WDE.PacketViewer.Utils;

public static class TimeSpanExtensions
{
    public static string ToHumanFriendlyString(this TimeSpan ts)
    {
        if (ts.Days > 0)
            return $"{ts.Days}d {ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.Hours > 0)
            return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";
        if (ts.Minutes > 0)
            return $"{ts.Minutes}m {ts.Seconds}s";
        if (ts.Seconds > 0)
            return $"{ts.Seconds}s {ts.Milliseconds}ms";
        if (ts.Milliseconds > 0)
            return $"{ts.Milliseconds}ms";
        return $"{ts.TotalMicroseconds} μs";
    }
}