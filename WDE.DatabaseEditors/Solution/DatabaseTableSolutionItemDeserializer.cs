using System.Linq;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemDeserializer : ISolutionItemDeserializer<DatabaseTableSolutionItem>, ISolutionItemSerializer<DatabaseTableSolutionItem>
    {
        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            solutionItem = null;
            if (projectItem.Type == 32 && projectItem.Comment != null)
            {
                var split = projectItem.Comment.Split(':');
                if (split.Length != 2)
                    return false;

                var table = split[0];
                var items = split[1].Split(',')
                    .Where(i => uint.TryParse(i, out var _))
                    .Select(uint.Parse)
                    .Select(i => new SolutionItemDatabaseEntity(i, true));

                var dbItem = new DatabaseTableSolutionItem(table);
                dbItem.Entries.AddRange(items);
                solutionItem = dbItem;
                return true;
            }

            return false;
        }

        public ISmartScriptProjectItem Serialize(DatabaseTableSolutionItem item)
        {
            var items = string.Join(",", item.Entries.Select(e => e.Key));
            return new AbstractSmartScriptProjectItem()
            {
                Type = 32,
                Comment = $"{item.DefinitionId}:{items}"
            };
        }
    }
}