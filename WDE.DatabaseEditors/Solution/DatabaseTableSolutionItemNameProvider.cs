using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemNameProvider : ISolutionNameProvider<DatabaseTableSolutionItem>,
        ISolutionNameProviderAsync<DatabaseTableSolutionItem>
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
            var definition = tableDefinitionProvider.GetDefinition(item.TableName);
            if (definition == null)
                return $"Unknown item (" + item.TableName + ")";

            var parameter = parameterFactory.Factory(definition.Picker);

            if (item.Entries.Count == 1)
            {
                var name = parameter.ToString(item.Entries[0].Key[0]);
                return definition.SingleSolutionName.Replace("{name}", name).Replace("{key}", item.Entries[0].Key.ToString());
            }

            return definition.MultiSolutionName;
        }

        public async Task<string> GetNameAsync(DatabaseTableSolutionItem item)
        {
            var definition = tableDefinitionProvider.GetDefinition(item.TableName);
            if (definition == null)
                return $"Unknown item (" + item.TableName + ")";

            var parameter = parameterFactory.Factory(definition.Picker);

            if (item.Entries.Count == 1)
            {
                string name;
                if (parameter is IAsyncParameter<long> asyncParameter)
                {
                    name = await asyncParameter.ToStringAsync(item.Entries[0].Key[0], default);
                }
                else
                {
                    name = parameter.ToString(item.Entries[0].Key[0]);
                }

                return definition.SingleSolutionName.Replace("{name}", name).Replace("{key}", item.Entries[0].Key.ToString());
            }

            return definition.MultiSolutionName;
        }
    }
}