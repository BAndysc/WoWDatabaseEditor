using System;
using System.Globalization;
using System.Windows.Data;
using ICSharpCode.AvalonEdit.Document;
using WDE.Common.WPF.Types;

namespace WDE.Common.WPF.Converters
{
    public class NativeTextDocumentConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NativeTextDocument text)
                return text.Native;
            return null;
        }

        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TextDocument doc)
                return new NativeTextDocument(doc);
            return null;
        }
    }
}