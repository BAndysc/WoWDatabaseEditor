using System;

namespace WDE.SmartScriptEditor.Debugging;

public class ScriptDebuggingDisabledException : Exception
{
    public ScriptDebuggingDisabledException(string message) : base("Script debugging is disabled: " + message)
    {
    }
}