using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemNameProvider : ISolutionNameProvider<DatabaseTableSolutionItem>
    {
        private readonly IParameterFactory parameterFactory;
        private readonly IDatabaseProvider databaseProvider;

        public DatabaseTableSolutionItemNameProvider(IParameterFactory parameterFactory, IDatabaseProvider databaseProvider)
        {
            this.parameterFactory = parameterFactory;
            this.databaseProvider = databaseProvider;
        }
        
        public string GetName(DatabaseTableSolutionItem item) => $"{item.TableId} ({item.ExtraId})";
    }
}