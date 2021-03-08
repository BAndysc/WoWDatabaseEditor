using System;
using System.Threading.Tasks;
using WDE.Common;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class CreatureTemplateTableProvider : DbEditorsSolutionItemProvider
    {
        private readonly Lazy<IDbEditorTableDataProvider> tableDataProvider;
        private readonly Lazy<ICreatureEntryProviderService> creatureEntryProviderService;

        public CreatureTemplateTableProvider(Lazy<IDbEditorTableDataProvider> tableDataProvider, Lazy<ICreatureEntryProviderService> creatureEntryProviderService) : 
            base("Creature Template", "Edit or create data of creature.", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.creatureEntryProviderService = creatureEntryProviderService;
        }


        public override async Task<ISolutionItem> CreateSolutionItem()
        {
            var key = await creatureEntryProviderService.Value.GetEntryFromService();
            if (key.HasValue)
            {
                var data = await tableDataProvider.Value.LoadCreatureTamplateDataEntry(key.Value);
                return new DbEditorsSolutionItem(data as DbTableData);
            }

            return null;
        }
    }
}