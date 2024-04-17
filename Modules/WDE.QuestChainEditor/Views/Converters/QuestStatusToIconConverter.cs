using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.Common.Types;
using WDE.QuestChainEditor.Services;

namespace WDE.QuestChainEditor.Views.Converters;

public class QuestStatusToIconConverter : IMultiValueConverter
{
    public static QuestStatusToIconConverter Instance { get; } = new();

    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 2 &&
            values[0] is QuestStatus status &&
            values[1] is bool canStart)
        {
            switch (status)
            {
                case QuestStatus.None:
                    return new ImageUri(canStart ? "Icons/icon_quest_can_take.png" : "Icons/icon_quest_cant_take.png");
                case QuestStatus.Complete:
                    return new ImageUri("Icons/icon_quest_completed_2.png");
                case QuestStatus.Incomplete:
                    return new ImageUri("Icons/icon_quest_incomplete_2.png");
                case QuestStatus.Failed:
                    return new ImageUri("Icons/icon_fail.png");
                case QuestStatus.Rewarded:
                    return new ImageUri("Icons/icon_ok.png");
                default:
                    return ImageUri.Empty;
            }
        }

        return ImageUri.Empty;
    }
}