using WDE.Common.Database;

namespace WDE.SmartScriptEditor.Debugging;

public readonly struct SavedScriptBreakpoint
{
    public readonly int EntryOrGuid;
    public readonly SmartScriptType ScriptType;
    public readonly int EventId;
    public readonly SmartBreakpointType BreakpointType;
    public readonly SmartBreakpointFlags Flags;

    public SavedScriptBreakpoint(int entryOrGuid, SmartScriptType scriptType, int eventId, SmartBreakpointType breakpointType, SmartBreakpointFlags flags)
    {
        EntryOrGuid = entryOrGuid;
        ScriptType = scriptType;
        EventId = eventId;
        BreakpointType = breakpointType;
        Flags = flags;
    }
}