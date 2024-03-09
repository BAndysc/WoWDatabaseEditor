using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Serilog.Events;

namespace WoWDatabaseEditorCore.Services.LogService.Logging;

public class DataStoreLoggerSink : ILogEventSink
{
    protected readonly Func<ILogDataStore> dataStoreProvider;
    
    private readonly IFormatProvider? formatProvider;

    public DataStoreLoggerSink(Func<ILogDataStore> dataStoreProvider,
                               IFormatProvider? formatProvider = null)
    {
        this.formatProvider = formatProvider;
        this.dataStoreProvider = dataStoreProvider;
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

        EventId eventId = EventIdFactory(logEvent);

        string message = logEvent.RenderMessage(formatProvider);
        
        string exception = logEvent.Exception?.Message ?? (logEvent.Level >= LogEventLevel.Error ? message : string.Empty);

        AddLogEntry(logLevel, eventId, message, exception);
    }

    protected virtual void AddLogEntry(LogLevel logLevel, EventId eventId, string message, string exception)
    {
        ILogDataStore? dataStore = dataStoreProvider.Invoke();

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (dataStore == null)
            return; // app is shutting down

        dataStore.AddEntry(new()
        {
            Timestamp = DateTime.UtcNow,
            LogLevel = logLevel,
            EventId = eventId,
            State = message,
            Exception = exception
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