using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectorOffsetConverter : IValueConverter
{
    public static ConnectorOffsetConverter Instance { get; } = new();

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double offset = parameter== null ? 0 : System.Convert.ToDouble(parameter);
        if (value is Size s)
        {
            return new Size((s.Width + offset) / 2, (s.Height + offset) / 2);
        }

        return new Size(offset / 2, offset / 2);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        double offset =  parameter== null ? 0 : System.Convert.ToDouble(parameter);
        if (value is Size s)
        {
            return new Size((s.Width + offset) / 2, (s.Height + offset) / 2);
        }

        return new Size(offset / 2, offset / 2);
    }
}