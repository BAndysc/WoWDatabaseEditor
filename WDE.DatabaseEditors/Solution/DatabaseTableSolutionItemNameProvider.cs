using System;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemNameProvider : ISolutionNameProvider<DatabaseTableSolutionItem>
    {
        private readonly Lazy<IParameterFactory> parameterFactory;
        private readonly IDatabaseProvider databaseProvider;

        public DatabaseTableSolutionItemNameProvider(Lazy<IParameterFactory> parameterFactory, IDatabaseProvider databaseProvider)
        {
            this.parameterFactory = parameterFactory;
            this.databaseProvider = databaseProvider;
        }
        
        public string GetName(DatabaseTableSolutionItem item) => $"{item.TableId} ({item.Entry})";
    }
}