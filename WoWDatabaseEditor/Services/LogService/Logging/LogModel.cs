using System;
using Microsoft.Extensions.Logging;

namespace WoWDatabaseEditorCore.Services.LogService.Logging;

public class LogModel
{
    #region Properties

    public DateTime Timestamp { get; set; }

    public LogLevel LogLevel { get; set; }

    public EventId EventId { get; set; }

    public object? State { get; set; }

    public string? Exception { get; set; }

    #endregion
}