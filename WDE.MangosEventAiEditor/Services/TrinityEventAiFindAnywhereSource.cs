using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.EventAiEditor;
using WDE.EventAiEditor.Data;
using WDE.EventAiEditor.Editor;
using WDE.EventAiEditor.Services;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Services;

[AutoRegisterToParentScope]
public class TrinityEventAiFindAnywhereSource : EventAiBaseFindAnywhereSource
{
    public TrinityEventAiFindAnywhereSource(IEventAiDataManager eventAiDataManager, IEventAiDatabaseProvider databaseProvider, IEditorFeatures editorFeatures, ISolutionItemNameRegistry nameRegistry, ISolutionItemIconRegistry iconRegistry) : base(eventAiDataManager, databaseProvider, editorFeatures, nameRegistry, iconRegistry)
    {
    }

    protected override ISolutionItem GenerateSolutionItem(IEventAiLine line)
    {
        return new EventAiSolutionItem(line.CreatureIdOrGuid);
    }
}