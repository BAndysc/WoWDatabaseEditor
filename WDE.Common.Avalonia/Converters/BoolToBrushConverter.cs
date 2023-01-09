using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Converters;

public class BoolToBrushConverter : IValueConverter
{
    public IBrush WhenTrue { get; set; } = Brushes.White;
    public IBrush WhenFalse { get; set; } = Brushes.White;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? WhenTrue : WhenFalse;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}