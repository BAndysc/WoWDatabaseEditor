using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class NullConverter : IValueConverter
    {
        public static NullConverter IsNull = new NullConverter();
        public static NullConverter IsNotNull = new NullConverter(){Inverted = true};
        
        public bool Inverted { get; set; }
        
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isNull = value == null;
            return Inverted ? !isNull : isNull;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new Exception();
        }
    }
}