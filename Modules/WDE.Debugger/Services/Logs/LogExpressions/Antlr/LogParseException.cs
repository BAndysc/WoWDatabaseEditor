using System;

namespace WDE.Debugger.Services.Logs.LogExpressions.Antlr;

internal class LogParseException : Exception
{
    public LogParseException(string msg) : base(msg)
    {
    }
}