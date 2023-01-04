using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class DataTimeToStringConverter : IValueConverter
    {
        public string Format { get; set; } = "MM/dd/yyyy HH:mm";
        
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString(Format);
            return "not a date";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new Exception();
        }
    }
}
