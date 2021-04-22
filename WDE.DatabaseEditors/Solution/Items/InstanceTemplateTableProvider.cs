using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Parameters;
using WDE.Common.Providers;
using WDE.DatabaseEditors.Loaders;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution.Items
{
    [AutoRegister]
    public class InstanceTemplateTableProvider : DatabaseTableSolutionItemProvider
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IItemFromListProvider itemFromListProvider;
        private readonly IParameterFactory parameterFactory;

        public InstanceTemplateTableProvider(IDatabaseTableDataProvider tableDataProvider, IItemFromListProvider itemFromListProvider, IParameterFactory parameterFactory) : 
            base("Instance Template", "Edit or create instance data.", "SmartScriptGeneric")
        {
            this.tableDataProvider = tableDataProvider;
            this.itemFromListProvider = itemFromListProvider;
            this.parameterFactory = parameterFactory;
        }

        public override async Task<ISolutionItem?> CreateSolutionItem()
        {
            var mapParameter = parameterFactory.Factory("MapParameter");
            var key = await itemFromListProvider.GetItemFromList(mapParameter.Items, false);
            if (key.HasValue)
            {
                var data = await tableDataProvider.Load("instance_template", (uint)key.Value);
                if (data != null)
                    return new DatabaseTableSolutionItem((uint)key.Value, "instance_template");
            }

            return null;
        }
    }
}