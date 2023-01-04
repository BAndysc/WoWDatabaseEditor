using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class BoolToRowDefinitionsConverter : IValueConverter
    {
        public RowDefinitions TrueValue { get; set; } = new();
        public RowDefinitions FalseValue { get; set; } = new();
        
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueValue : FalseValue;
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    public class BoolToColumnDefinitionsConverter : IValueConverter
    {
        public ColumnDefinitions TrueValue { get; set; } = new();
        public ColumnDefinitions FalseValue { get; set; } = new();
        
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? TrueValue : FalseValue;
            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
