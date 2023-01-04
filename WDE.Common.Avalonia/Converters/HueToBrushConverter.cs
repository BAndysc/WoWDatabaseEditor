using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using AvaloniaStyles.Controls;
using AvaloniaStyles.Utils;
using HslColor = AvaloniaStyles.Utils.HslColor;

namespace WDE.Common.Avalonia.Converters;

public class HueToBrushConverter : IValueConverter
{
    public double Saturation { get; set; }
    public double Lightness { get; set; }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return new SolidColorBrush(new HslColor(d, Saturation, Lightness).ToRgba());
        }

        return new SolidColorBrush(Colors.White);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
