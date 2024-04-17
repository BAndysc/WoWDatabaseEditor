using System;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToStrokeDashArrayConverter : IValueConverter
{
    public AvaloniaList<double>? Completed { get; set; }
    public AvaloniaList<double>? MustBeActive { get; set; }
    public AvaloniaList<double>? Breadcrumb { get; set; } = new AvaloniaList<double>() { 1, 1 };

    public static ConnectionTypeToStrokeDashArrayConverter Instance { get; } = new ConnectionTypeToStrokeDashArrayConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is QuestRequirementType type)
        {
            return type switch
            {
                QuestRequirementType.Completed => Completed,
                QuestRequirementType.MustBeActive => MustBeActive,
                QuestRequirementType.Breadcrumb => Breadcrumb,
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