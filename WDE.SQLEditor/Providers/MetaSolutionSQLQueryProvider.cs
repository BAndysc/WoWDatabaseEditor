using System.Threading.Tasks;
using WDE.Common.Solution;
using WDE.Module.Attributes;

namespace WDE.SQLEditor.Providers
{
    [AutoRegister]
    public class MetaSolutionSQLQueryProvider : ISolutionItemSqlProvider<MetaSolutionSQL>
    {
        public async Task<string> GenerateSql(MetaSolutionSQL item)
        {
            return item.GetSql();
        }
    }
}