using System;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;

namespace AvaloniaStyles.Converters;

public class NativeMenuItemToggleTypeToMenuItemToggleTypeConverter : IValueConverter
{
    public static NativeMenuItemToggleTypeToMenuItemToggleTypeConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is NativeMenuItemToggleType nativeMenuType)
        {
            return nativeMenuType switch
            {
                NativeMenuItemToggleType.None => MenuItemToggleType.None,
                NativeMenuItemToggleType.Radio => MenuItemToggleType.Radio,
                NativeMenuItemToggleType.CheckBox => MenuItemToggleType.CheckBox,
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