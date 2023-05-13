using System;
using System.Diagnostics;
using WDE.Module.Attributes;

namespace WDE.MySqlDatabaseCommon.Services
{
    public enum QueryType
    {
        Unknown,
        WriteQuery,
        ReadQuery
    }
    
    [UniqueProvider]
    public interface IDatabaseLogger
    {
        event Action<string?, string?, TraceLevel, QueryType> OnLog;
    }

    [AutoRegister]
    [SingleInstance]
    public class DatabaseLogger : IDatabaseLogger
    {
        public event Action<string?, string?, TraceLevel, QueryType>? OnLog;

        public void Log(string? messsage, string? category, TraceLevel level)
        {
            OnLog?.Invoke(messsage, category, level, QueryType.Unknown);
        }
        
        public void Log(string? messsage, string? category, TraceLevel level, QueryType queryType)
        {
            OnLog?.Invoke(messsage, category, level, queryType);
        }
    }
}