using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters;

public class NullDoubleConverter : IValueConverter
{
    public static NullDoubleConverter WhenNullThen0 = new NullDoubleConverter(){ WhenNull = 0, WhenNotNull = 1 };
    public static NullDoubleConverter WhenNullThen1 = new NullDoubleConverter(){ WhenNull = 1, WhenNotNull = 0 };
        
    public double WhenNull { get; set; }
    public double WhenNotNull { get; set; }
        
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value == null ? WhenNull : WhenNotNull;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new Exception();
    }
}