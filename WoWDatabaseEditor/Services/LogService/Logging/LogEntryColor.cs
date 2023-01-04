using Avalonia.Media;

namespace WoWDatabaseEditorCore.Services.LogService.Logging;

public struct LogEntryColor
{
    public LogEntryColor()
    {
    }

    public IBrush Foreground { get; set; } = Brushes.Black;
    public IBrush Background { get; set; } = Brushes.Transparent;
}