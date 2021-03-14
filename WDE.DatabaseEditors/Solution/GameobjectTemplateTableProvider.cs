using System;
using System.Threading.Tasks;
using WDE.Common;
using WDE.DatabaseEditors.Data;
using WDE.DatabaseEditors.Models;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class GameobjectTemplateTableProvider : DbEditorsSolutionItemProvider
    {
        private readonly Lazy<IDbEditorTableDataProvider> tableDataProvider;
        private readonly Lazy<IGameobjectEntryProviderService> gameobjectEntryProviderService;

        public GameobjectTemplateTableProvider(Lazy<IDbEditorTableDataProvider> tableDataProvider, Lazy<IGameobjectEntryProviderService> gameobjectEntryProviderService) : 
            base("Gameobject Template", "Edit or create data of gameobject.", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.gameobjectEntryProviderService = gameobjectEntryProviderService;
        }

        public override async Task<ISolutionItem> CreateSolutionItem()
        {
            var key = await gameobjectEntryProviderService.Value.GetEntryFromService();
            if (key.HasValue)
            {
                var data = await tableDataProvider.Value.LoadGameobjectTemplateDataEntry(key.Value);
                if (data != null)
                    return new DbEditorsSolutionItem(key.Value,
                        nameof(tableDataProvider.Value.LoadGameobjectTemplateDataEntry), data);
            }

            return null;
        }
    }
}