using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Solution;
using WDE.Common.Tasks;
using WDE.LootEditor.Editor.ViewModels;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.Solution;

[AutoRegister]
public class LootSolutionItemQueryGenerator : ISolutionItemSqlProvider<LootSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public LootSolutionItemQueryGenerator(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }
    
    public async Task<IQuery> GenerateSql(LootSolutionItem item)
    {
        using var viewModel = containerProvider.Resolve<LootEditorViewModel>((typeof(LootSolutionItem), item));
        await viewModel.Load();
        return await viewModel.GenerateQuery();
    }
}