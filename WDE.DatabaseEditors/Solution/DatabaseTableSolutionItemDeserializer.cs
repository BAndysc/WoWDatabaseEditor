using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
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
            if (projectItem.Type == 32 && projectItem.StringValue != null)
            {
                var split = projectItem.StringValue.Split(':');
                if (split.Length != 2)
                    return false;

                var table = split[0];
                var items = split[1].Split(',')
                    .Where(i => uint.TryParse(i, out var _))
                    .Select(uint.Parse)
                    .Select(i => new SolutionItemDatabaseEntity(i, true));

                var dbItem = new DatabaseTableSolutionItem(table);
                if (string.IsNullOrEmpty(projectItem.Comment))
                {
                    dbItem.Entries.AddRange(items);
                }
                else
                {
                    var entries = JsonConvert.DeserializeObject<List<SolutionItemDatabaseEntity>>(projectItem.Comment);
                    dbItem.Entries.AddRange(entries);
                }
                solutionItem = dbItem;
                return true;
            }

            return false;
        }

        public ISmartScriptProjectItem Serialize(DatabaseTableSolutionItem item)
        {
            var entries = JsonConvert.SerializeObject(item.Entries);
            var items = string.Join(",", item.Entries.Select(e => e.Key));
            return new AbstractSmartScriptProjectItem()
            {
                Type = 32,
                StringValue = $"{item.DefinitionId}:{items}",
                Comment = entries
            };
        }
    }
}