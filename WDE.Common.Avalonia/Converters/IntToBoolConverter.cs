using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public int TrueValue { get; set; } = 1;
        public int FalseValue { get; set; } = 0;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int l)
                return l == TrueValue;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueValue : FalseValue;
            return FalseValue;
        }
    }
}