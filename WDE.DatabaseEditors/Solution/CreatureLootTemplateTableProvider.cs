using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Providers;
using WDE.DatabaseEditors.Data;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class CreatureLootTemplateTableProvider : DbEditorsSolutionItemProvider
    {
        private readonly IDbEditorTableDataProvider tableDataProvider;
        private readonly ICreatureEntryProviderService creatureEntryProviderService;

        public CreatureLootTemplateTableProvider(IDbEditorTableDataProvider tableDataProvider, ICreatureEntryProviderService creatureEntryProviderService) : 
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
                    return new DbEditorsSolutionItem(key.Value, "creature_loot_template");
            }

            return null;
        }
    }
}