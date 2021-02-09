using System;
using System.Globalization;
using System.Windows.Data;

namespace WoWDatabaseEditorCore.WPF.Views.Helpers
{
    public class DocumentOrToolToTitleConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length != 3)
                return "";

            if (!(values[0] is string title))
                return "";

            bool? isModified = null;
            bool? isLoading = null;

            if (values[1] is bool modified)
                isModified = modified;
            
            if (values[2] is bool loading)
                isLoading = loading;

            if (isLoading ?? false)
                return $"{title} (loading)";
            
            if (isModified ?? false)
                return $"{title}*";
            
            return title;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}