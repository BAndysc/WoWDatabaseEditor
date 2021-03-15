using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WDE.Common.WPF.ViewHelpers
{
    public class NullToVisibilityConverter : IValueConverter
    {
        public Visibility WhenNull { get; set; } = Visibility.Hidden;
        public Visibility WhenNotNull { get; set; } = Visibility.Visible;
        
        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? WhenNull : WhenNotNull;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}