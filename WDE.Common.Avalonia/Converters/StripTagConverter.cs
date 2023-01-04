using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.Common.Utils;

namespace WDE.Common.Avalonia.Converters
{
    public class StripTagConverter : IValueConverter
    {
        public static StripTagConverter Instance { get; } = new();

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string s)
                return s.RemoveTags();
            return value;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}