using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class LongToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long l)
                return l != 0;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? 1L : 0L;
            return 0L;
        }
    }
}