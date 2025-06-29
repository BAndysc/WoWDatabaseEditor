using System;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToStrokeDashArrayConverter : IValueConverter
{
    public AvaloniaList<double>? Completed { get; set; }
    public AvaloniaList<double>? MustBeActive { get; set; }
    public AvaloniaList<double>? Breadcrumb { get; set; } = new AvaloniaList<double>() { 1, 1 };
    public AvaloniaList<double>? FactionChange { get; set; } = new AvaloniaList<double>() { 4,4 };

    public static ConnectionTypeToStrokeDashArrayConverter Instance { get; } = new ConnectionTypeToStrokeDashArrayConverter();

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