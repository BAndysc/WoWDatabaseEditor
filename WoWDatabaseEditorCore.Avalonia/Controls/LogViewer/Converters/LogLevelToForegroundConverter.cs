using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using WoWDatabaseEditorCore.Services.LogService.Logging;

namespace WoWDatabaseEditorCore.Avalonia.Controls.LogViewer.Converters;

public class LogLevelToForegroundConverter : IValueConverter
{
    public struct LogEntryColor
    {
        public LogEntryColor()
        {
        }

        public IBrush? Foreground { get; set; } = Brushes.Black;
        public IBrush? Background { get; set; } = Brushes.Transparent;
    }

    public static Dictionary<LogLevel, LogEntryColor> Colors { get; } = new()
    {
        [LogLevel.Trace] = new() { Foreground = Brushes.DarkGray },
        [LogLevel.Debug] = new() { Foreground = Brushes.Gray },
        [LogLevel.Information] = new(),
        [LogLevel.Warning] = new() { Foreground = Brushes.Orange},
        [LogLevel.Error] = new() { Foreground = Brushes.White, Background = Brushes.OrangeRed },
        [LogLevel.Critical] = new() { Foreground = Brushes.White, Background = Brushes.Red },
        [LogLevel.None] = new(),
    };

    public static LogLevelToForegroundConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogLevel l)
        {
            return Colors[l].Foreground;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class LogLevelToBackgroundConverter : IValueConverter
{
    public static LogLevelToBackgroundConverter Instance { get; } = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LogLevel l)
        {
            return LogLevelToForegroundConverter.Colors[l].Background;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}