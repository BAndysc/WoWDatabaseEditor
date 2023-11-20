using System.Linq;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.DatabaseEditors.Extensions;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Solution
{
    [AutoRegister]
    public class DatabaseTableSolutionItemSqlProvider : ISolutionItemSqlProvider<DatabaseTableSolutionItem>
    {
        private readonly IDatabaseTableDataProvider tableDataProvider;
        private readonly IQueryGenerator queryGenerator;

        public DatabaseTableSolutionItemSqlProvider(IDatabaseTableDataProvider tableDataProvider, 
            IQueryGenerator queryGenerator)
        {
            this.tableDataProvider = tableDataProvider;
            this.queryGenerator = queryGenerator;
        }

        public async Task<IQuery> GenerateSql(DatabaseTableSolutionItem item)
        {
            IDatabaseTableData? tableData = await LoadTable(item);

            if (tableData == null)
                return Queries.Raw(DataDatabaseType.World, "-- Unable to load data for {item} from the database");

            item.UpdateEntitiesWithOriginalValues(tableData.Entities);
            return queryGenerator.GenerateQuery(item.Entries.Select(e => e.Key).ToList(), item.DeletedEntries, tableData);
        }

        private Task<IDatabaseTableData?> LoadTable(DatabaseTableSolutionItem item)
        {
            //todo single record scenario?
            return tableDataProvider.Load(item.TableName, null, null,null ,item.Entries.Select(e => e.Key).ToArray());
        }
    }
}