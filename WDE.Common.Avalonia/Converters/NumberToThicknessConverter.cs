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
    
    public double ConstLeft { get; set; }
    public double ConstTop { get; set; }
    public double ConstRight { get; set; }
    public double ConstBottom { get; set; }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int i)
        {
            return new Thickness(Left * i + ConstLeft, Top * i + ConstTop, Right * i + ConstRight, Bottom * i + ConstBottom);
        }
        else if (value is uint ui)
        {
            return new Thickness(Left * ui + ConstLeft, Top * ui + ConstTop, Right * ui + ConstRight, Bottom * ui + ConstBottom);
        }
        else if (value is float f)
        {
            return new Thickness(Left * f + ConstLeft, Top * f + ConstTop, Right * f + ConstRight, Bottom * f + ConstBottom);
        }
        else if (value is double d)
        {
            return new Thickness(Left * d + ConstLeft, Top * d + ConstTop, Right * d + ConstRight, Bottom * d + ConstBottom);
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}