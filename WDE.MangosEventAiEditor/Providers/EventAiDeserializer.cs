using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.EventAiEditor;
using WDE.Module.Attributes;

namespace WDE.MangosEventAiEditor.Providers
{
    [AutoRegisterToParentScope]
    [SingleInstance]
    public class EventAiDeserializer : ISolutionItemDeserializer<EventAiSolutionItem>, ISolutionItemSerializer<EventAiSolutionItem>
    {
        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            solutionItem = null;
            if (projectItem.Type != 130)
                return false;

            solutionItem = new EventAiSolutionItem(projectItem.Value);
            return true;
        }

        public ISmartScriptProjectItem? Serialize(EventAiSolutionItem item, bool forMostRecentlyUsed)
        {
            return new AbstractSmartScriptProjectItem()
            {
                Type = 130,
                Value = item.EntryOrGuid
            };
        }
    }
}