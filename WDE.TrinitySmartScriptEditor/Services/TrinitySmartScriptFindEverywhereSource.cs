using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;
using WDE.SmartScriptEditor.Data;
using WDE.SmartScriptEditor.Editor;
using WDE.SmartScriptEditor.Services;

namespace WDE.TrinitySmartScriptEditor.Services;

[AutoRegisterToParentScope]
public class TrinitySmartScriptFindAnywhereSource : SmartScriptBaseFindAnywhereSource
{
    public TrinitySmartScriptFindAnywhereSource(ISmartDataManager smartDataManager, ISmartScriptDatabaseProvider databaseProvider, IEditorFeatures editorFeatures, ISolutionItemNameRegistry nameRegistry, ISolutionItemIconRegistry iconRegistry) : base(smartDataManager, databaseProvider, editorFeatures, nameRegistry, iconRegistry)
    {
    }

    protected override ISolutionItem GenerateSolutionItem(ISmartScriptLine line)
    {
        return new SmartScriptSolutionItem(line.EntryOrGuid, (SmartScriptType)line.ScriptSourceType);
    }
}