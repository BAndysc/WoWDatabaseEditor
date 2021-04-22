using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemNameProvider : ISolutionNameProvider<DatabaseTableSolutionItem>
    {
        private readonly IParameterFactory parameterFactory;
        private readonly ITableDefinitionProvider tableDefinitionProvider;
        private readonly IDatabaseProvider databaseProvider;

        public DatabaseTableSolutionItemNameProvider(IParameterFactory parameterFactory, 
            ITableDefinitionProvider tableDefinitionProvider,
            IDatabaseProvider databaseProvider)
        {
            this.parameterFactory = parameterFactory;
            this.tableDefinitionProvider = tableDefinitionProvider;
            this.databaseProvider = databaseProvider;
        }

        public string GetName(DatabaseTableSolutionItem item)
        {
            var definition = tableDefinitionProvider.GetDefinition(item.DefinitionId);
            if (definition == null)
                return $"Unknown item (" + item.DefinitionId + ")";

            var parameter = parameterFactory.Factory(definition.Picker);

            if (item.Entries.Count == 1)
            {
                var name = parameter.ToString(item.Entries[0].Key);
                return definition.SingleSolutionName.Replace("{name}", name);
            }

            return definition.MultiSolutionName;
        }
    }
}