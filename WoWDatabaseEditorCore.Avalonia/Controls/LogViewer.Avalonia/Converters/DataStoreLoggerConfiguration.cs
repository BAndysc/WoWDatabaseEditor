using System.Collections.Generic;
using Avalonia.Media;
using Microsoft.Extensions.Logging;
using WoWDatabaseEditorCore.Services.LogService.Logging;

namespace WoWDatabaseEditorCore.Avalonia.Controls.LogViewer.Avalonia.Converters;

public class DataStoreLoggerConfiguration
{
    #region Properties
    
    public EventId EventId { get; set; }

    public Dictionary<LogLevel, LogEntryColor> Colors { get; } = new()
    {
        [LogLevel.Trace] = new() { Foreground = Brushes.DarkGray },
        [LogLevel.Debug] = new() { Foreground = Brushes.Gray },
        [LogLevel.Information] = new(),
        [LogLevel.Warning] = new() { Foreground = Brushes.Orange},
        [LogLevel.Error] = new() { Foreground = Brushes.White, Background = Brushes.OrangeRed },
        [LogLevel.Critical] = new() { Foreground = Brushes.White, Background = Brushes.Red },
        [LogLevel.None] = new(),
    };

    #endregion
}