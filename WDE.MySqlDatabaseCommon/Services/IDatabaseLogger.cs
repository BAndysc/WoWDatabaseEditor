using System;
using System.Diagnostics;
using WDE.Module.Attributes;

namespace WDE.MySqlDatabaseCommon.Services
{
    [UniqueProvider]
    public interface IDatabaseLogger
    {
        event Action<string?, string?, TraceLevel> OnLog;
    }

    [AutoRegister]
    [SingleInstance]
    public class DatabaseLogger : IDatabaseLogger
    {
        public event Action<string?, string?, TraceLevel>? OnLog;

        public void Log(string? messsage, string? category, TraceLevel level)
        {
            OnLog?.Invoke(messsage, category, level);
        }
    }
}