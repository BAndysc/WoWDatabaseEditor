using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.Common.Types;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Views;

public class MySqlFileImportStateConverter : IValueConverter
{
    public static MySqlFileImportStateConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is MySqlFileImportState state)
        {
            return state switch
            {
                MySqlFileImportState.Finished => new ImageUri("Icons/icon_ok.png"),
                MySqlFileImportState.Failed => new ImageUri("Icons/icon_fail.png"),
                _ => ImageUri.Empty
            };
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}