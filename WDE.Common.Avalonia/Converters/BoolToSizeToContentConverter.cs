using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters;

public class BoolToSizeToContentConverter : IValueConverter
{
    public SizeToContent WhenTrue { get; set; }
    public SizeToContent WhenFalse { get; set; }

    public static BoolToSizeToContentConverter Default { get; } = new BoolToSizeToContentConverter()
        { WhenTrue = SizeToContent.WidthAndHeight, WhenFalse = SizeToContent.Manual };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return b ? WhenTrue : WhenFalse;
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}