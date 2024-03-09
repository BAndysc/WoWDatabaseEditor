
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonAreaTriggerCreateProperties : IAreaTriggerCreateProperties
{
    public uint Id { get; set; }

    
    public uint AreaTriggerId { get; set; }

    
    public string ScriptName { get; set; } = "";
}