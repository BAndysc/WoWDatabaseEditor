using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters;

public class MultiplyNumberConverter : IValueConverter
{
    public double MultiplyBy { get; set; }
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return d * MultiplyBy;
        if (value is float f)
            return (float)(f * MultiplyBy);
        if (value is int i)
            return (int)(i * MultiplyBy);
        if (value is uint u)
            return (uint)(u * MultiplyBy);
        if (value is long l)
            return (long)(l * MultiplyBy);
        if (value is ulong ul)
            return (ulong)(ul * MultiplyBy);
        throw new NotImplementedException();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}