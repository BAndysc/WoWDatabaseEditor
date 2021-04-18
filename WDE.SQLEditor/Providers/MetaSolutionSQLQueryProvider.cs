using System;
using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.Module.Attributes;

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
        
        public async Task<string> GenerateSql(MetaSolutionSQL item)
        {
            return await sqlGeneratorRegistry.Value.GenerateSql(item.ItemToGenerate);
        }
    }
}
