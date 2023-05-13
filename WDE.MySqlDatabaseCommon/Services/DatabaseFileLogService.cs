using System.Diagnostics;
using WDE.Common.Services;
using WDE.Module.Attributes;

namespace WDE.MySqlDatabaseCommon.Services;

[AutoRegister]
public class DatabaseFileLogService
{
    private const string LogFolder = "~/database_logs/";

    private RollingLogFile logger;

    public DatabaseFileLogService(IDatabaseLogger logger,
        IFileSystem fileSystem)
    {
        logger.OnLog += LoggerOnOnLog;
        this.logger = new RollingLogFile(fileSystem.ResolvePhysicalPath(LogFolder).FullName);
    }
    
    private void LoggerOnOnLog(string? arg1, string? arg2, TraceLevel arg3, QueryType queryType)
    {
        // this is kind of a cheat here, but I just know that Unknown query types are not worth logging
        // because they are gonna be reads
        if (queryType == QueryType.WriteQuery)
            logger.WriteLog($"{arg1} {arg2}\n\n");
    }
}