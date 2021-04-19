using System;
using System.Threading.Tasks;
using WDE.Common;
using WDE.DatabaseEditors.Loaders;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution.Items
{
    [AutoRegister]
    public class GameobjectTemplateTableProvider : DatabaseTableSolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IGameobjectEntryProviderService gameobjectEntryProviderService;

        public GameobjectTemplateTableProvider(IDatabaseTableDataProvider tableDataProvider, IGameobjectEntryProviderService gameobjectEntryProviderService) : 
            base("Gameobject Template", "Edit or create data of gameobject.", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.gameobjectEntryProviderService = gameobjectEntryProviderService;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var key = await gameobjectEntryProviderService.GetEntryFromService();
            if (key.HasValue)
            {
                var data = await tableDataProvider.Load("gameobject_template", key.Value);
                if (data != null)
                    return new DatabaseTableSolutionItem(key.Value, "gameobject_template");
            }

            return null;
        }
    }
}