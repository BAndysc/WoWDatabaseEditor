using System;

namespace WDE.SmartScriptEditor.Debugging;

// Serialized, don't change values
[Flags]
[Serializable]
public enum SmartBreakpointFlags
{
    None = 0,
    DontSuspendExecution = 1,
    Log = 2,
    NoStacktrace = 4,
    Disabled = 8,
}