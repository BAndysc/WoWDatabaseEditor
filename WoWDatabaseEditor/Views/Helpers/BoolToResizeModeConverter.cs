using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WoWDatabaseEditor.Views.Helpers
{
    public class BoolToResizeModeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? ResizeMode.CanResize : ResizeMode.NoResize;
            }

            return ResizeMode.CanResize;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}