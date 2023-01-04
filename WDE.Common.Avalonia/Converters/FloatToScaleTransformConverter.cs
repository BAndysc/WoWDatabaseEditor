using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace WDE.Common.Avalonia.Converters;

public class FloatToScaleTransformConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is float f)
        {
            return new ScaleTransform(f, f);
        }

        return null;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
