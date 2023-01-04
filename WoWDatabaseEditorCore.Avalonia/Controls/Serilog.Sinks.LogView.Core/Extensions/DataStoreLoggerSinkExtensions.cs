using System;
using Serilog;
using Serilog.Configuration;
using WoWDatabaseEditorCore.Avalonia.Controls.LogViewer.Avalonia.Converters;
using WoWDatabaseEditorCore.Services.LogService.Logging;

namespace WoWDatabaseEditorCore.Avalonia.Controls.Serilog.Sinks.LogView.Core.Extensions;

public static class DataStoreLoggerSinkExtensions
{
    public static LoggerConfiguration DataStoreLoggerSink
    (
        this LoggerSinkConfiguration loggerConfiguration,
        Func<ILogDataStore> dataStoreProvider, 
        Action<DataStoreLoggerConfiguration>? configuration = null,
        IFormatProvider formatProvider = null!
    )
        => loggerConfiguration.Sink(new DataStoreLoggerSink(dataStoreProvider, GetConfig(configuration), formatProvider));

    private static Func<DataStoreLoggerConfiguration> GetConfig(Action<DataStoreLoggerConfiguration>? configuration)
    {
        // convert from Action to Func delegate to pass data
        DataStoreLoggerConfiguration data = new();
        configuration?.Invoke(data);
        return () => data;
    }
}