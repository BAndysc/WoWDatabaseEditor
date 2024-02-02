using System;

namespace WDE.SmartScriptEditor.Debugging;

// Serialized, don't change values
[Serializable]
public enum SmartBreakpointType
{
    Any = 0,
    Event = 1,
    Source = 2,
    Target = 3,
    Action = 4
}