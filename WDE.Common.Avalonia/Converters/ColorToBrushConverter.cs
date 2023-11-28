using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Converters;

public class ColorToBrushConverter : IValueConverter
{
    public static ColorToBrushConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Color color)
            return new SolidColorBrush(color);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}