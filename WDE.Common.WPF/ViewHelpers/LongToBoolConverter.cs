using System;
using System.Globalization;
using System.Windows.Data;

namespace WDE.Common.WPF.ViewHelpers
{
    public class LongToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                var longVal = System.Convert.ToInt64(value);
                return longVal > 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                bool boolVal = System.Convert.ToBoolean(value);
                return boolVal ? 1 : 0;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}