using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters;

public class NumberToThicknessConverter : IValueConverter
{
    public double Left { get; set; }
    public double Top { get; set; }
    public double Right { get; set; }
    public double Bottom { get; set; }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
        {
            return new Thickness(Left * i, Top * i, Right * i, Bottom * i);
        }
        else if (value is uint ui)
        {
            return new Thickness(Left * ui, Top * ui, Right * ui, Bottom * ui);
        }
        else if (value is float f)
        {
            return new Thickness(Left * f, Top * f, Right * f, Bottom * f);
        }
        else if (value is double d)
        {
            return new Thickness(Left * d, Top * d, Right * d, Bottom * d);
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}