using System.IO;
using WDE.Module.Attributes;

namespace WDE.EventScriptsEditor.EventScriptData;

[AutoRegister]
[SingleInstance]
public class EventScriptDataJsonProvider : IEventScriptDataJsonProvider
{
    public string GetJson() => File.ReadAllText("EventScriptData/EventScriptCommands.json");
}