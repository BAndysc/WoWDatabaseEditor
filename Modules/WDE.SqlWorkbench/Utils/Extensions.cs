using System;
using MySqlConnector;

namespace WDE.SqlWorkbench.Utils;

public static class Extensions
{
    public static string ToPrettyString(this MySqlDateTime dateTime, bool dateOnly)
    {
        if (dateOnly)
            return $"{dateTime.Year:0000}-{dateTime.Month:00}-{dateTime.Day:00}";
            
        var dateTimeString =$"{dateTime.Year:0000}-{dateTime.Month:00}-{dateTime.Day:00} {dateTime.Hour:00}:{dateTime.Minute:00}:{dateTime.Second:00}";
        if (dateTime.Microsecond > 0)
            dateTimeString += $".{dateTime.Microsecond:000000}";
        return dateTimeString;
    }

    public static string ToPrettyString(this TimeSpan time)
    {
        var sign = time.Ticks < 0 ? "-" : "";
        var hours = Math.Abs((int)time.TotalHours);
        var minutes = Math.Abs(time.Minutes);
        var seconds = Math.Abs(time.Seconds);
        var micro = Math.Abs(time.Milliseconds) * 1000 + Math.Abs(time.Microseconds);
        
        if (micro > 0)
            return $"{sign}{hours:00}:{minutes:00}:{seconds:00}.{micro:000000}";
        return $"{sign}{hours:00}:{minutes:00}:{seconds:00}";
    }
}