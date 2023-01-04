using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public string? WhenTrue { get; set; }
        public string? WhenFalse { get; set; }
            
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? WhenTrue : WhenFalse;
            return null;
        }
    
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
