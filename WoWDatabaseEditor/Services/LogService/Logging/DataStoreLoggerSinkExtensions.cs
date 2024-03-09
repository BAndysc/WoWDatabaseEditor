using System;
using Serilog;
using Serilog.Configuration;

namespace WoWDatabaseEditorCore.Services.LogService.Logging;

public static class DataStoreLoggerSinkExtensions
{
    public static LoggerConfiguration DataStoreLoggerSink
    (
        this LoggerSinkConfiguration loggerConfiguration,
        Func<ILogDataStore> dataStoreProvider, 
        IFormatProvider formatProvider = null!
    )
        => loggerConfiguration.Sink(new DataStoreLoggerSink(dataStoreProvider, formatProvider));
}