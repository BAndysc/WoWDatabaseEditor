using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace AvaloniaStyles.Converters;

public class IntToHexStringConverter : IValueConverter
{
    public static IntToHexStringConverter Instance { get; } = new IntToHexStringConverter();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
            return $"0x{i:X}";
        if (value is uint ui)
            return $"0x{ui:X}";
        if (value is long l)
            return $"0x{l:X}";
        if (value is ulong ul)
            return $"0x{ul:X}";
        throw new InvalidCastException("Cannot convert " + value?.GetType().Name + " to hex string");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}