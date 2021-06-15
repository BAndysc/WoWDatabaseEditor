using System;
using System.Text;
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
            StringBuilder sb = new();
            foreach (var subitem in item.ItemsToGenerate)
                sb.AppendLine(await sqlGeneratorRegistry.Value.GenerateSql(subitem));
            
            return sb.ToString();
        }
    }
}
