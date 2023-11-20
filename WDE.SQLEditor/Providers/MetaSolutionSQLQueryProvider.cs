using System;
using System.Text;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSolutionSQLQueryProvider : ISolutionItemSqlProvider<MetaSolutionSQL>
    {
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorRegistry;

        public MetaSolutionSQLQueryProvider(Lazy<ISolutionItemSqlGeneratorRegistry> sqlGeneratorRegistry)
        {
            this.sqlGeneratorRegistry = sqlGeneratorRegistry;
        }
        
        public async Task<IQuery> GenerateSql(MetaSolutionSQL item)
        {
            IMultiQuery multiQuery = Queries.BeginTransaction(DataDatabaseType.World);
            foreach (var subitem in item.ItemsToGenerate)
                multiQuery.Add(await sqlGeneratorRegistry.Value.GenerateSql(subitem));

            return multiQuery.Close();
        }
    }
}
