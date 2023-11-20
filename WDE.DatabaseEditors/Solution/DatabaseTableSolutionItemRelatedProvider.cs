using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    [SingleInstance]
    public class DatabaseTableSolutionItemRelatedProvider : ISolutionItemRelatedProvider<DatabaseTableSolutionItem>
    {
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private Dictionary<DatabaseTable, RelatedSolutionItem.RelatedType> definitionToRelatedType = new();
        
        public DatabaseTableSolutionItemRelatedProvider(ITableDefinitionProvider tableDefinitionProvider)
        {
            this.tableDefinitionProvider = tableDefinitionProvider;
            foreach (var defi in tableDefinitionProvider.Definitions)
            {
                if (defi.Picker == "CreatureParameter")
                    definitionToRelatedType[defi.Id] = RelatedSolutionItem.RelatedType.CreatureEntry;
                else if (defi.Picker == "GameobjectParameter")
                    definitionToRelatedType[defi.Id] = RelatedSolutionItem.RelatedType.GameobjectEntry;
                else if (defi.Picker == "GossipMenuParameter")
                    definitionToRelatedType[defi.Id] = RelatedSolutionItem.RelatedType.GossipMenu;
                else if (defi.Picker == "QuestParameter")
                    definitionToRelatedType[defi.Id] = RelatedSolutionItem.RelatedType.QuestEntry;
            }
        }

        public Task<RelatedSolutionItem?> GetRelated(DatabaseTableSolutionItem item)
        {
            if (item.Entries.Count == 1 &&
                definitionToRelatedType.TryGetValue(item.TableName, out var type))
                return Task.FromResult<RelatedSolutionItem?>(new RelatedSolutionItem(type, item.Entries[0].Key[0]));
            return Task.FromResult<RelatedSolutionItem?>(null);
        }
    }
}