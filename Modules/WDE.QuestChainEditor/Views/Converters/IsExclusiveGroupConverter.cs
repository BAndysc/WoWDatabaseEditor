using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.QuestChainEditor.ViewModels;

namespace WDE.QuestChainEditor.Views.Converters;

public class IsExclusiveGroupConverter : IValueConverter
{
    public static IsExclusiveGroupConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is ExclusiveGroupViewModel;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}