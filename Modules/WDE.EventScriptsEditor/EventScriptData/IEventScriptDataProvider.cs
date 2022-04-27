using System.Collections.Generic;

namespace WDE.EventScriptsEditor.EventScriptData;

public interface IEventScriptDataProvider
{
    IReadOnlyList<EventScriptRawData> GetEventScriptData();
    EventScriptRawData? GetEventScriptData(uint id);
}