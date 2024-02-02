using System;

namespace WDE.SmartScriptEditor.Debugging;

public class ScriptDebuggingErrorException : Exception
{
    public ScriptDebuggingErrorException(string message) : base(message)
    {
    }
}