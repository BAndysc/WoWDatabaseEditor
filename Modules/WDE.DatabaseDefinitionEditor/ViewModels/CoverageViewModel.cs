using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Services;
using WDE.Module.Attributes;

namespace WDE.DatabaseDefinitionEditor.ViewModels;

[AutoRegister]
public class CoverageViewModel
{
    private readonly IDatabaseQueryExecutor mySqlExecutor;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly ITableDefinitionProvider tableDefinitionProvider;

    public ObservableCollection<DatabaseTable> MissingTables { get; } = new();
    public ObservableCollection<DatabaseTable> CoveredTables { get; } = new();
    
    public CoverageViewModel(IDatabaseQueryExecutor mySqlExecutor, 
        ICurrentCoreVersion currentCoreVersion,
        ITableDefinitionProvider tableDefinitionProvider)
    {
        this.mySqlExecutor = mySqlExecutor;
        this.currentCoreVersion = currentCoreVersion;
        this.tableDefinitionProvider = tableDefinitionProvider;
        PopulateTables(DataDatabaseType.World).ListenErrors();
        PopulateTables(DataDatabaseType.Hotfix).ListenErrors();
    }

    public async Task PopulateTables(DataDatabaseType type)
    {
        var tables = await mySqlExecutor.GetTables(type);
        var current = tableDefinitionProvider.Definitions
            .Select(x => new DatabaseTable(x.DataDatabaseType, x.TableName))
            .Union(tableDefinitionProvider.Definitions
                .Where(x => x.ForeignTableByName != null)
                .SelectMany(x => x.ForeignTableByName!.Keys.Select(table => new DatabaseTable(x.DataDatabaseType, table))))
            .ToList();
        MissingTables.AddRange<DatabaseTable>(tables.Except(current));
        CoveredTables.AddRange<DatabaseTable>(tables.Intersect(current));
    }
}