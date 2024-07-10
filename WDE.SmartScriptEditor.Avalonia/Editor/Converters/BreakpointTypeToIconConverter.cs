using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.Common.Types;
using WDE.SmartScriptEditor.Debugging;

namespace WDE.SmartScriptEditor.Avalonia.Editor.Converters;

public class BreakpointTypeToIconConverter : IValueConverter
{
    public static BreakpointTypeToIconConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SmartBreakpointType bt)
        {
            switch (bt)
            {
                case SmartBreakpointType.Any:
                    return new ImageUri("Icons/icon_mini_breakpoint_all.png");
                case SmartBreakpointType.Event:
                    return new ImageUri("Icons/icon_mini_breakpoint_event.png");
                case SmartBreakpointType.Target:
                    return new ImageUri("Icons/icon_mini_breakpoint_target.png");
                case SmartBreakpointType.Source:
                    return new ImageUri("Icons/icon_mini_breakpoint_source.png");
                case SmartBreakpointType.Action:
                    return new ImageUri("Icons/icon_mini_breakpoint_action.png");
                default:
                    return new ImageUri("Icons/`.png");
            }
        }
        return new ImageUri("Icons/icon_mini_breakpoint.png");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}