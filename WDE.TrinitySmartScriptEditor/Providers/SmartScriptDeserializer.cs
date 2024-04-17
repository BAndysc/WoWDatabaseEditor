using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SmartScriptEditor;

namespace WDE.TrinitySmartScriptEditor.Providers
{
    [AutoRegisterToParentScope]
    [SingleInstance]
    public class SmartScriptDeserializer : ISolutionItemDeserializer<SmartScriptSolutionItem>, ISolutionItemSerializer<SmartScriptSolutionItem>
    {
        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            solutionItem = null;
            if (projectItem.Type < 100 || projectItem.Type >= 100 + (int) SmartScriptType.END)
                return false;

            solutionItem = new SmartScriptSolutionItem(projectItem.Value, (SmartScriptType) (projectItem.Type - 100));
            return true;
        }

        public ISmartScriptProjectItem? Serialize(SmartScriptSolutionItem item, bool forMostRecentlyUsed)
        {
            return new AbstractSmartScriptProjectItem()
            {
                Type = (byte)(100 + (byte) item.SmartType),
                Value = item.EntryOrGuid
            };
        }
    }
}