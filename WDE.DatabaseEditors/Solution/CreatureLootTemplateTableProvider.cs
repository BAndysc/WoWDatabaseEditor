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
        private readonly Lazy<IDbEditorTableDataProvider> tableDataProvider;
        private readonly Lazy<IInputEntryProvider> inputEntryProvider;

        public CreatureLootTemplateTableProvider(Lazy<IDbEditorTableDataProvider> tableDataProvider, Lazy<IInputEntryProvider> inputEntryProvider) : 
            base("Creature Loot Template", "Edit or create loot data of creature.", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.inputEntryProvider = inputEntryProvider;
        }

        public override async Task<ISolutionItem> CreateSolutionItem()
        {
            var key = await inputEntryProvider.Value.GetEntry();
            if (key.HasValue)
            {
                var data = await tableDataProvider.Value.LoadCreatureLootTemplateData(key.Value);
                if (data != null)
                    return new DbEditorsSolutionItem(key.Value, DbTableContentType.CreatureLootTemplate, true, 
                        new Dictionary<string, DbTableSolutionItemModifiedField>());
            }

            return null;
        }
    }
}