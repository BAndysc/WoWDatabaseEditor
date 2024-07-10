using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Services;
using WDE.DatabaseEditors.Data.Structs;
using WDE.DatabaseEditors.Loaders;
using WDE.DatabaseEditors.Models;
using WDE.DatabaseEditors.QueryGenerators;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.DatabaseEditors.Services;

public interface ITableEntityDuplicatorService
{
    public Task<IQuery?> GenerateDuplicationQuery(DatabaseTable table, DatabaseKey originalKey, DatabaseKey newKey);
}

[AutoRegister]
[SingleInstance]
public class TableEntityDuplicatorService : ITableEntityDuplicatorService
{
    private readonly IDatabaseTableDataProvider dataProvider;
    private readonly IQueryGenerator queryGenerator;

    public TableEntityDuplicatorService(IDatabaseTableDataProvider dataProvider,
        IQueryGenerator queryGenerator)
    {
        this.dataProvider = dataProvider;
        this.queryGenerator = queryGenerator;
    }

    public async Task<IQuery?> GenerateDuplicationQuery(DatabaseTable table, DatabaseKey originalKey, DatabaseKey newKey)
    {
        var data = await dataProvider.Load(table, null, 0, 1, new[]{originalKey});
        if (data == null || data.Entities.Count == 0)
            return null;

        if (data.TableDefinition.GroupByKeys.Count != newKey.Count)
            return null;

        var newEntity = data.Entities[0].Clone(newKey, false);
        int i = 0;
        foreach (var column in data.TableDefinition.GroupByKeys)
            newEntity.SetTypedCellOrThrow(column, newKey[i++]);
        IQuery insert = queryGenerator.GenerateQuery(new[] { newKey }, null, new DatabaseTableData(data.TableDefinition, new[] { newEntity }));
        return insert;
    }
}