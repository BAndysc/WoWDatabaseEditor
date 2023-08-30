using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return !b;
            return false;
        }
    }
}