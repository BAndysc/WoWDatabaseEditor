using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Converters;

public class BoolToFontWeightConverter : IValueConverter
{
    public static BoolToFontWeightConverter Bold { get; } = new BoolToFontWeightConverter(){WhenTrue = FontWeight.Bold, WhenFalse = FontWeight.Normal};
    
    public FontWeight WhenFalse { get; set; }

    public FontWeight WhenTrue { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && b)
            return WhenTrue;
        return WhenFalse;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}