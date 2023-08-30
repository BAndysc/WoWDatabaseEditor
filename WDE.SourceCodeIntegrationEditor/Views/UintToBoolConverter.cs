using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WDE.SourceCodeIntegrationEditor.Views
{
    public class UintToBoolConverter : IValueConverter
    {
        public uint TrueValue { get; set; }
        
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is uint u)
                return u == TrueValue;
            return false;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}