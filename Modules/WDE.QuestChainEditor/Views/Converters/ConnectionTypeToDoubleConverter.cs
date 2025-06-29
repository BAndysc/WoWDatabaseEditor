using System;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using Nodify;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToDoubleConverter : IValueConverter
{
    public double Completed { get; set; } = 0.5;
    public double MustBeActive { get; set; } = 0.1;
    public double Breadcrumb { get; set; } = 0.5;
    public double FactionChange { get; set; } = 0.5;

    public static ConnectionTypeToDoubleConverter Instance { get; } = new ConnectionTypeToDoubleConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConnectionType type)
        {
            return type switch
            {
                ConnectionType.Completed => Completed,
                ConnectionType.MustBeActive => MustBeActive,
                ConnectionType.Breadcrumb => Breadcrumb,
                ConnectionType.FactionChange => FactionChange,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}