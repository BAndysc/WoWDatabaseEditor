using System;
using System.Globalization;
using System.Windows.Data;

namespace WDE.Common.WPF.Converters
{
    public class DataTimeToStringConverter : IValueConverter
    {
        public string Format { get; set; } = "m";
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dt)
                return dt.ToString(Format);
            return "not a date";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}