using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class BoolToDoubleConverter : IValueConverter
    {
        public double WhenTrue { get; set; } = 1;
        public double WhenFalse { get; set; } = 0;

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
                flag = (bool) value;
            return flag ? WhenTrue : WhenFalse;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d == WhenTrue)
                    return true;
                if (d == WhenFalse)
                    return false;
            }

            return false;
        }
    }
}
