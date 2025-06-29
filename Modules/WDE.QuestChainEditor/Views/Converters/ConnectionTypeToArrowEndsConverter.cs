using System;
using System.Globalization;
using Avalonia.Collections;
using Avalonia.Data.Converters;
using Nodify;
using WDE.QuestChainEditor.Models;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views.Converters;

public class ConnectionTypeToArrowEndsConverter : IValueConverter
{
    public ArrowHeadEnds Completed { get; set; } = ArrowHeadEnds.End;
    public ArrowHeadEnds MustBeActive { get; set; } = ArrowHeadEnds.End; // could be Start if users prefer
    public ArrowHeadEnds Breadcrumb { get; set; } = ArrowHeadEnds.End;
    public ArrowHeadEnds FactionChange { get; set; } = ArrowHeadEnds.Both;

    public static ConnectionTypeToArrowEndsConverter Instance { get; } = new ConnectionTypeToArrowEndsConverter();

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