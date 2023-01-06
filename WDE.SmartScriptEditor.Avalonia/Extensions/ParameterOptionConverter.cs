using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.SmartScriptEditor.Editor.ViewModels.Editing;

namespace WDE.SmartScriptEditor.Avalonia.Extensions;

public class ParameterOptionConverter : IValueConverter
{
    public static ParameterOptionConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ParameterOption option)
            return option.Value.ToString();
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}