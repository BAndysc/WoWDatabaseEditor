using System;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data.Converters;

namespace WDE.Common.Avalonia.Converters
{
    public class BoolToScrollBarVisibilityConverter : IValueConverter
    {
        public ScrollBarVisibility WhenFalse { get; set; }
        public ScrollBarVisibility WhenTrue { get; set; }
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? WhenTrue : WhenFalse;
            return WhenFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}