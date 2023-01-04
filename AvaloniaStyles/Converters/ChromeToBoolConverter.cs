using System;
using System.Globalization;
using Avalonia.Data.Converters;
using AvaloniaStyles.Controls;

namespace AvaloniaStyles.Converters;

public class ChromeToBoolConverter : IValueConverter
{
    public bool NoChrome { get; set; }
    public bool AlwaysChrome { get; set; }
    public bool MacChromeOnly { get; set; }
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ExtendedWindowChrome chrome)
        {
            if (chrome == ExtendedWindowChrome.AlwaysSystemChrome)
                return AlwaysChrome;
            if (chrome == ExtendedWindowChrome.NoSystemChrome)
                return NoChrome;
            if (chrome == ExtendedWindowChrome.MacOsChrome)
                return MacChromeOnly;
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
