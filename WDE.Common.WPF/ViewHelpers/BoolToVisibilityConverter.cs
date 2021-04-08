using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WDE.Common.WPF.ViewHelpers
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public Visibility WhenTrue { get; set; } = Visibility.Visible;
        public Visibility WhenFalse { get; set; } = Visibility.Hidden;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var flag = false;
            if (value is bool)
                flag = (bool) value;
            return flag ? WhenTrue : WhenFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility vis)
            {
                if (vis == WhenTrue)
                    return true;
                if (vis == WhenFalse)
                    return false;
            }

            return false;
        }
    }
}