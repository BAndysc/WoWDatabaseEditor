using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Converters;

public class RgbColorToBrushConverter : IValueConverter
{
    public static RgbColorToBrushConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is RgbColor rgbColor)
            return new SolidColorBrush(Color.FromArgb(255, rgbColor.R, rgbColor.G, rgbColor.B));
        if (value is Color color)
            return new SolidColorBrush(color);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}