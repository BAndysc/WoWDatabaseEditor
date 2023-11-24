using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.LootEditor.Editor.ViewModels;
using WDE.LootEditor.Solution.PerDatabaseTable;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.Solution.PerEntity;

[AutoRegister]
public class PerEntityLootSolutionItemQueryGenerator : ISolutionItemSqlProvider<PerEntityLootSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public PerEntityLootSolutionItemQueryGenerator(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }
    
    public async Task<IQuery> GenerateSql(PerEntityLootSolutionItem item)
    {
        using var viewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(PerEntityLootSolutionItem), item), (typeof(PerDatabaseTableLootSolutionItem), null));
        await viewModel.Load();
        return await viewModel.GenerateQuery();
    }
}