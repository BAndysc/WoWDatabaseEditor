using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToBrushConverter : IValueConverter
{
    public IBrush? Completed { get; set; }
    public IBrush? MustBeActive { get; set; }
    public IBrush? Breadcrumb { get; set; }
    public IBrush? FactionChange { get; set; }

    public static ConnectionTypeToBrushConverter Instance { get; } = new ConnectionTypeToBrushConverter();

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
        if (value is QuestRequirementType type2)
        {
            return type2 switch
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