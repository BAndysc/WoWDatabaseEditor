using System;
using System.Threading.Tasks;
using Prism.Ioc;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.LootEditor.Editor.ViewModels;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.LootEditor.Solution.PerDatabaseTable;

[AutoRegister]
public class PerDatabaseTableLootSolutionItemQueryGenerator : ISolutionItemSqlProvider<PerDatabaseTableLootSolutionItem>
{
    private readonly IContainerProvider containerProvider;

    public PerDatabaseTableLootSolutionItemQueryGenerator(IContainerProvider containerProvider)
    {
        this.containerProvider = containerProvider;
    }
    
    public async Task<IQuery> GenerateSql(PerDatabaseTableLootSolutionItem item)
    {
        throw new Exception("THIS SHOULD BE NEVER CALLED! (PerDatabaseTableLootSolutionItemQueryGenerator) can't be part of the SolutionExplorer");
    }
}