using System;
using System.Text;

namespace WDE.Common.Utils;

public static class TimeExtensions
{
    // returns a string that is a human readable representation of the time span
    public static string ToNiceString(this TimeSpan span)
    {
        if (span == TimeSpan.MaxValue)
            return "∞";
        if (span == TimeSpan.MinValue)
            return "-∞";

        var sb = new StringBuilder();

        if (span.Days > 0)
            sb.Append(span.Days).Append("d ");

        if (span.Hours > 0)
            sb.Append(span.Hours).Append("h ");

        if (span.Minutes > 0)
            sb.Append(span.Minutes).Append("m ");

        if (span.Seconds > 0)
            sb.Append(span.Seconds).Append("s ");

        if (sb.Length > 0)
            sb.Remove(sb.Length - 1, 1);
        else
            return $"{span.TotalMilliseconds}ms";
        
        return sb.ToString();
    }
}