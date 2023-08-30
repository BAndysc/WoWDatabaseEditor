using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Converters;

public class NumberToNiceBytesConverter : IValueConverter
{
    public static readonly NumberToNiceBytesConverter Instance = new();
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ulong l)
        {
            return l.ToHumanFriendlyFileSize();
        }

        if (value is long ul)
        {
            if (ul >= 0)
                return ((ulong)ul).ToHumanFriendlyFileSize();
            return "-" + ((ulong)-ul).ToHumanFriendlyFileSize();
        }

        if (value is float f)
        {
            return Convert((long)f, targetType, parameter, culture);
        }

        if (value is float d)
        {
            return Convert((long)d, targetType, parameter, culture);
        }

        return null!;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}