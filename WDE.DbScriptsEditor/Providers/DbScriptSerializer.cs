using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.DbScriptsEditor.Models;
using WDE.Module.Attributes;

namespace WDE.DbScriptsEditor.Providers
{
    [AutoRegister]
    [SingleInstance]
    public class DbScriptSerializer : ISolutionItemDeserializer<DbScriptSolutionItem>, ISolutionItemSerializer<DbScriptSolutionItem>
    {
        private const byte ProjectItemType = 131;

        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            solutionItem = null;
            if (projectItem.Type != ProjectItemType)
                return false;

            var scriptType = (DbScriptType)(projectItem.Value2 ?? 0);
            solutionItem = new DbScriptSolutionItem(scriptType, (uint)projectItem.Value);
            return true;
        }

        public ISmartScriptProjectItem? Serialize(DbScriptSolutionItem item, bool forMostRecentlyUsed)
        {
            return new AbstractSmartScriptProjectItem()
            {
                Type = ProjectItemType,
                Value = (int)item.ScriptId,
                Value2 = (int)item.ScriptType
            };
        }
    }
}
