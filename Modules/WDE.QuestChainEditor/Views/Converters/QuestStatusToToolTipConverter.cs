using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.Common.Types;
using WDE.QuestChainEditor.Services;

namespace WDE.QuestChainEditor.Views.Converters;

public class QuestStatusToToolTipConverter : IMultiValueConverter
{
    public static QuestStatusToToolTipConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 3 &&
            values[0] is QuestStatus status &&
            values[1] is bool canStart)
        {
            var reason = values[2] as string;
            switch (status)
            {
                case QuestStatus.None:
                    return canStart ? "Can take" : "Can't take:\n" + reason;
                case QuestStatus.Complete:
                    return "Completed";
                case QuestStatus.Incomplete:
                    return "Incomplete";
                case QuestStatus.Failed:
                    return "Failed";
                case QuestStatus.Rewarded:
                    return "Rewarded";
                default:
                    return null;
            }
        }

        return null;
    }
}