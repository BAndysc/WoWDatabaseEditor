using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToBrushConverter : IValueConverter
{
    public IBrush? Completed { get; set; }
    public IBrush? MustBeActive { get; set; }
    public IBrush? Breadcrumb { get; set; }

    public static ConnectionTypeToBrushConverter Instance { get; } = new ConnectionTypeToBrushConverter();

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