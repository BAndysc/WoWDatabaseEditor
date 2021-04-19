using System.Threading.Tasks;
using WDE.Common;
using WDE.DatabaseEditors.Loaders;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution.Items
{
    [AutoRegister]
    public class CreatureLootTemplateTableProvider : DatabaseTableSolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly ICreatureEntryProviderService creatureEntryProviderService;

        public CreatureLootTemplateTableProvider(IDatabaseTableDataProvider tableDataProvider, ICreatureEntryProviderService creatureEntryProviderService) : 
            base("Creature Loot Template", "Edit or create loot data of creature.", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.creatureEntryProviderService = creatureEntryProviderService;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var key = await creatureEntryProviderService.GetEntryFromService();
            if (key.HasValue)
            {
                var data = await tableDataProvider.Load("creature_loot_template", key.Value);
                if (data != null)
                    return new DatabaseTableSolutionItem(key.Value, "creature_loot_template");
            }

            return null;
        }
    }
}