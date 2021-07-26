using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class EmptyStringToBoolConverter : IValueConverter
    {
        public bool WhenNullOrEmpty { get; set; }
        
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string s)
                return string.IsNullOrEmpty(s) ? WhenNullOrEmpty : !WhenNullOrEmpty;
            return value == null ? WhenNullOrEmpty : false;
        }

        public object ConvertBack(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}