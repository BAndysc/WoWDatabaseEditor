using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaEdit.Document;

namespace WDE.Common.Avalonia.Converters;

public class StringToDocumentConverter : IValueConverter
{
    public static readonly StringToDocumentConverter Instance = new();
        
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text)
            return new TextDocument(text);
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}