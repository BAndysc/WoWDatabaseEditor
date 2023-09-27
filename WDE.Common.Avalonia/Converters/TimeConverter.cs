using System;
using System.Globalization;
using System.Text;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters;

public class TimeConverter : IValueConverter
{
    private static StringBuilder sb = new StringBuilder();
    public TimeConverterSourceUnits Units { get; set; } = TimeConverterSourceUnits.Minutes;
    
    public static TimeConverter FromMinutes { get; } = new() { Units = TimeConverterSourceUnits.Minutes };
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        long number = 0;
        if (value is int i)
            number = i;
        else if (value is long l)
            number = l;
        else if (value is short s)
            number = s;
        else if (value is byte b)
            number = b;
        else if (value is uint ui)
            number = ui;
        else if (value is ushort us)
            number = us;
        else if (value is sbyte sb)
            number = sb;
        else
            throw new NotImplementedException();

        long days = 0, hours = 0, minutes = 0, seconds = 0;
        if (Units == TimeConverterSourceUnits.Minutes)
            minutes = number;
        
        if (minutes > 60)
        {
            hours = minutes / 60;
            minutes = minutes % 60;
        }
        
        if (hours > 24)
        {
            days = hours / 24;
            hours = hours % 24;
        }

        sb.Clear();
        if (days > 0)
            sb.Append($"{days}d ");
        if (hours > 0)
            sb.Append($"{hours}h ");
        if (minutes > 0)
            sb.Append($"{minutes}m ");
        if (seconds > 0)
            sb.Append($"{seconds}s ");
        return sb.ToString().Trim();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            string[] parts = s.Split(' ');
            long days = 0, hours = 0, minutes = 0, seconds = 0;
            foreach (var part in parts)
            {
                if (part.EndsWith("d"))
                    days = long.Parse(part.Substring(0, part.Length - 1));
                else if (part.EndsWith("h"))
                    hours = long.Parse(part.Substring(0, part.Length - 1));
                else if (part.EndsWith("m"))
                    minutes = long.Parse(part.Substring(0, part.Length - 1));
                else if (part.EndsWith("s"))
                    seconds = long.Parse(part.Substring(0, part.Length - 1));
                else
                    throw new NotImplementedException();
            }

            if (Units == TimeConverterSourceUnits.Minutes)
                return days * 24 * 60 + hours * 60 + minutes;
            else
                throw new NotImplementedException();
        }
        throw new NotImplementedException();
    }
}

public enum TimeConverterSourceUnits
{
    Minutes
}