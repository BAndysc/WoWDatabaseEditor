using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class IntToBoolConverter : IValueConverter
    {
        public int TrueValue { get; set; } = 1;
        public int FalseValue { get; set; } = 0;
        public bool UnknownValues { get; set; } = false;

        
        private bool Convert(long number)
        {
            if (number == TrueValue)
                return true;
            if (number == FalseValue)
                return false;
            return UnknownValues;
        }

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int i)
                return Convert(i);
            if (value is long l)
                return Convert(l);
            return UnknownValues;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (targetType == typeof(int))
            {
                if (value is bool b)
                    return b ? TrueValue : FalseValue;
                return FalseValue;   
            }
            else if (targetType == typeof(long))
            {
                if (value is bool b)
                    return b ? (long)TrueValue : (long)FalseValue;
                return (long)FalseValue;
            }
            else
                throw new ArgumentOutOfRangeException(nameof(targetType), "Target type must be int or long");
        }
    }
}
