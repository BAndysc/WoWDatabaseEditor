using System.Threading.Tasks;
using WDE.Common;
using WDE.DatabaseEditors.Loaders;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution.Items
{
    [AutoRegister]
    public class QuestTemplateTableProvider : DatabaseTableSolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IQuestEntryProviderService questEntryProviderService;

        public QuestTemplateTableProvider(IDatabaseTableDataProvider tableDataProvider, IQuestEntryProviderService questEntryProviderService) : 
            base("Quest Template", "Edit quest templates", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.questEntryProviderService = questEntryProviderService;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var key = await questEntryProviderService.GetEntryFromService();
            if (key.HasValue)
            {
                var data = await tableDataProvider.Load("quest_template", key.Value);
                if (data != null)
                    return new DatabaseTableSolutionItem(key.Value, "quest_template");
            }

            return null;
        }
    }
}