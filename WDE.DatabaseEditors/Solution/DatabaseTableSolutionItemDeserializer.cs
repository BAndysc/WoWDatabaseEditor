using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemDeserializer : ISolutionItemDeserializer<DatabaseTableSolutionItem>, ISolutionItemSerializer<DatabaseTableSolutionItem>
    {
        private readonly TableDefinitionProvider tableDefinitionProvider;

        public DatabaseTableSolutionItemDeserializer(TableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
        }
        
        public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
        {
            solutionItem = null;
            if (projectItem.Type == 32 && projectItem.StringValue != null)
            {
                var split = projectItem.StringValue.Split(':');
                if (split.Length != 2 && split.Length != 3)
                    return false;

                var table = split[0];
                var items = split[1].Split(',')
                    .Where(i => DatabaseKey.TryDeserialize(i, out var _))
                    .Select(DatabaseKey.Deserialize)
                    .Select(i => new SolutionItemDatabaseEntity(i, true));
                var deletedEntries = split.Length == 2 ? null : split[2].Split(',')
                    .Where(i => DatabaseKey.TryDeserialize(i, out var _))
                    .Select(DatabaseKey.Deserialize);

                var definition = tableDefinitionProvider.GetDefinition(table);
                if (definition == null)
                    return false;
                
                var dbItem = new DatabaseTableSolutionItem(table, definition.IgnoreEquality);
                if (string.IsNullOrEmpty(projectItem.Comment))
                {
                    dbItem.Entries.AddRange(items);
                }
                else
                {
                    var entries = JsonConvert.DeserializeObject<List<SolutionItemDatabaseEntity>>(projectItem.Comment);
                    dbItem.Entries.AddRange(entries);
                }
                if (deletedEntries != null)
                    dbItem.DeletedEntries.AddRange(deletedEntries);
                solutionItem = dbItem;
                return true;
            }

            return false;
        }

        public ISmartScriptProjectItem Serialize(DatabaseTableSolutionItem item, bool forMostRecentlyUsed)
        {
            var entries = JsonConvert.SerializeObject(item.Entries, new JsonSerializerSettings()
            {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Converters = { new DatabaseKeyConverter() }
            });
            bool ignoreKeys = (forMostRecentlyUsed && item.IgnoreEquality);
            var items = ignoreKeys ? "" : string.Join(",", item.Entries.Select(e => e.Key.Serialize()));
            var deletedKeys = ignoreKeys ? "" : string.Join(",", item.DeletedEntries.Select(key => key.Serialize()));
            return new AbstractSmartScriptProjectItem()
            {
                Type = 32,
                StringValue = $"{item.DefinitionId}:{items}:{deletedKeys}",
                Comment = entries
            };
        }
    }
}