using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Converters;

public class ColorAwareBoolToBrushConverter : IValueConverter
{
    public IBrush WhenTrueDark { get; set; } = Brushes.White;
    public IBrush WhenFalseDark { get; set; } = Brushes.White;
    public IBrush WhenTrueLight { get; set; } = Brushes.Black;
    public IBrush WhenFalseLight { get; set; } = Brushes.Black;
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
            return AvaloniaStyles.SystemTheme.EffectiveThemeIsDark ? (b ? WhenTrueDark : WhenFalseDark) : (b ? WhenTrueLight : WhenFalseLight);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}