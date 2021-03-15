using System;
using System.Globalization;
using System.Windows.Data;

namespace WDE.Common.WPF.Converters
{
    public class NullConverter : IValueConverter
    {
        public bool Inverted { get; set; }
        
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isNull = value == null;
            return Inverted ? !isNull : isNull;
        }
        
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new Exception();
        }
    }
}