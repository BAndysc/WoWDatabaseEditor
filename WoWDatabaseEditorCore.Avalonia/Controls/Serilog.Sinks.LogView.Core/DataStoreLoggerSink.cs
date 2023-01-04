using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;
using WoWDatabaseEditorCore.Avalonia.Controls.LogViewer.Avalonia.Converters;
using WoWDatabaseEditorCore.Services.LogService.Logging;

namespace WoWDatabaseEditorCore.Avalonia.Controls.Serilog.Sinks.LogView.Core;

public class DataStoreLoggerSink : ILogEventSink
{
    protected readonly Func<ILogDataStore> _dataStoreProvider;
    
    private readonly IFormatProvider? _formatProvider;
    private readonly Func<DataStoreLoggerConfiguration>? _getCurrentConfig;

    public DataStoreLoggerSink(Func<ILogDataStore> dataStoreProvider,
                               Func<DataStoreLoggerConfiguration>? getCurrentConfig = null,
                               IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
        _dataStoreProvider = dataStoreProvider;
        _getCurrentConfig = getCurrentConfig;
    }

    public void Emit(LogEvent logEvent)
    {
        LogLevel logLevel = logEvent.Level switch
        {
            LogEventLevel.Verbose => LogLevel.Trace,
            LogEventLevel.Debug => LogLevel.Debug,
            LogEventLevel.Warning => LogLevel.Warning,
            LogEventLevel.Error => LogLevel.Error,
            LogEventLevel.Fatal => LogLevel.Critical,
            _ => LogLevel.Information
        };

        DataStoreLoggerConfiguration? config = _getCurrentConfig?.Invoke();

        EventId eventId = EventIdFactory(logEvent);
        if (eventId.Id == 0 && config?.EventId != 0)
            eventId = config?.EventId ?? 0;

        string message = logEvent.RenderMessage(_formatProvider);
        
        string exception = logEvent.Exception?.Message ?? (logEvent.Level >= LogEventLevel.Error ? message : string.Empty);

        LogEntryColor? color = config?.Colors[logLevel];

        AddLogEntry(logLevel, eventId, message, exception, color ?? new());
    }

    protected virtual void AddLogEntry(LogLevel logLevel, EventId eventId, string message, string exception, LogEntryColor color)
    {
        ILogDataStore? dataStore = _dataStoreProvider.Invoke();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (dataStore == null)
            return; // app is shutting down

        dataStore.AddEntry(new()
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel,
            EventId = eventId,
            State = message,
            Exception = exception,
            Color = color
        });
    }

    private static EventId EventIdFactory(LogEvent logEvent)
    {
        EventId eventId;
        if (!logEvent.Properties.TryGetValue("EventId", out LogEventPropertyValue? src))
            return new();
        
        int? id = null;
        string? eventName = null;

        // ref: https://stackoverflow.com/a/56722516
        StructureValue? value = src as StructureValue;

        LogEventProperty? idProperty = value!.Properties.FirstOrDefault(x => x.Name.Equals("Id"));
        if (idProperty is not null)
            id = int.Parse(idProperty.Value.ToString());

        LogEventProperty? nameProperty = value.Properties.FirstOrDefault(x => x.Name.Equals("Name"));
        if (nameProperty is not null)
            eventName = nameProperty.Value.ToString().Trim('"');

        eventId = new EventId(id ?? 0, eventName ?? string.Empty);

        return eventId;
    }
}