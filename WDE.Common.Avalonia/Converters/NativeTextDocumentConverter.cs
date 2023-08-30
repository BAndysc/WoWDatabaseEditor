using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaEdit.Document;
using WDE.Common.Avalonia.Types;

namespace WDE.Common.Avalonia.Converters
{
    public class NativeTextDocumentConverter : IValueConverter
    {
        public static readonly NativeTextDocumentConverter Instance = new();
        
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is NativeTextDocument text)
                return text.Native;
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is TextDocument doc)
                return new NativeTextDocument(doc);
            return null;
        }
    }
}