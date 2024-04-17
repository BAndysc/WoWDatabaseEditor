using System;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using Nodify;
using WDE.QuestChainEditor.Models;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToArrowEndsConverter : IValueConverter
{
    public ArrowHeadEnds Completed { get; set; } = ArrowHeadEnds.End;
    public ArrowHeadEnds MustBeActive { get; set; } = ArrowHeadEnds.Start;
    public ArrowHeadEnds Breadcrumb { get; set; } = ArrowHeadEnds.End;

    public static ConnectionTypeToArrowEndsConverter Instance { get; } = new ConnectionTypeToArrowEndsConverter();

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