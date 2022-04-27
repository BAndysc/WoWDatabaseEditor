using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaGraph.Controls;

namespace WDE.QuestChainEditor.Views;

public class AttachModeToBoolConverter : IValueConverter
{
    public ConnectorAttachMode TrueValue { get; set; }
    
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ConnectorAttachMode attachMode)
        {
            return attachMode == TrueValue;
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}