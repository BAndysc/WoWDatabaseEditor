using System;
using System.Text;
using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Solutions.Providers
{
    [AutoRegister]
    public class FolderSqlProvider : ISolutionItemSqlProvider<SolutionFolderItem>
    {
        private readonly Lazy<ISolutionItemSqlGeneratorRegistry> registry;

        public FolderSqlProvider(Lazy<ISolutionItemSqlGeneratorRegistry> registry)
        {
            this.registry = registry;
        }

        public async Task<IQuery> GenerateSql(SolutionFolderItem item)
        {
            IMultiQuery query = Queries.BeginTransaction(DataDatabaseType.World);
            query.Comment(item.MyName);
            foreach (ISolutionItem i in item.Items)
            {
                if (i.IsExportable)
                    query.Add(await registry.Value.GenerateSql(i));
            }

            return query.Close();
        }
    }
}