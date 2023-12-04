using System;
using System.Globalization;
using Avalonia.Data.Converters;
using WDE.SqlWorkbench.Services.Connection;

namespace WDE.SqlWorkbench.Settings;

public class SafeModeToNameConverter : IValueConverter
{
    public static SafeModeToNameConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is QueryExecutionSafety safety)
            return safety switch
            {
                QueryExecutionSafety.ExecuteAll => "Execute all",
                QueryExecutionSafety.AskUnlessSelect => "Ask unless SELECT",
                QueryExecutionSafety.AlwaysAsk => "Always ask",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        throw new ArgumentOutOfRangeException(nameof(value), value, null);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class SafeModeToDescriptionConverter : IValueConverter
{
    public static SafeModeToDescriptionConverter Instance { get; } = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is QueryExecutionSafety safety)
            return safety switch
            {
                QueryExecutionSafety.ExecuteAll => "All queries will be executed automatically",
                QueryExecutionSafety.AskUnlessSelect => "Require user confirmation before executing any queries, except for SELECT and SHOW",
                QueryExecutionSafety.AlwaysAsk => "Always require user confirmation before execution any queries",
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        throw new ArgumentOutOfRangeException(nameof(value), value, null);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}