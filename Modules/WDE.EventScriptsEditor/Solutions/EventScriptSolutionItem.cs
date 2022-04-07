using System.Collections.ObjectModel;
using WDE.Common;
using WDE.Common.Database;

namespace WDE.EventScriptsEditor.Solutions;

public class EventScriptSolutionItem : ISolutionItem
{
    public EventScriptType ScriptType { get; init; }
    public uint Id { get; init; }

    public EventScriptSolutionItem(EventScriptType scriptType, uint id)
    {
        ScriptType = scriptType;
        Id = id;
    }
    
    public bool IsContainer => false;
    public ObservableCollection<ISolutionItem>? Items => null;
    public string? ExtraId => null;
    public bool IsExportable => false;
    
    public ISolutionItem Clone()
    {
        return new EventScriptSolutionItem(ScriptType, Id);
    }
}