using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.CoreVersion;
using WDE.Common.Database;
using WDE.Common.Managers;
using WDE.Common.Types;
using WDE.Common.Utils;
using WDE.DatabaseEditors.Data.Interfaces;
using WDE.DatabaseEditors.Data.Structs;
using WDE.Module.Attributes;

namespace WDE.DatabaseEditors.Tools;

[AutoRegister]
public class CoverageViewModel
{
    private readonly IMySqlExecutor mySqlExecutor;
    private readonly ICurrentCoreVersion currentCoreVersion;
    private readonly ITableDefinitionProvider tableDefinitionProvider;

    public ObservableCollection<string> MissingTables { get; } = new();
    public ObservableCollection<string> CoveredTables { get; } = new();
    
    public CoverageViewModel(IMySqlExecutor mySqlExecutor, 
        ICurrentCoreVersion currentCoreVersion,
        ITableDefinitionProvider tableDefinitionProvider)
    {
        this.mySqlExecutor = mySqlExecutor;
        this.currentCoreVersion = currentCoreVersion;
        this.tableDefinitionProvider = tableDefinitionProvider;
        PopulateTables().ListenErrors();
    }

    public async Task PopulateTables()
    {
        var tables = await mySqlExecutor.GetTables();
        var current = tableDefinitionProvider.Definitions
            .Select(x => x.TableName)
            .Union(tableDefinitionProvider.Definitions
                .Where(x => x.ForeignTableByName != null)
                .SelectMany(x => x.ForeignTableByName!.Keys))
            .ToList();
        MissingTables.AddRange(tables.Except(current));
        CoveredTables.AddRange(tables.Intersect(current));
    }
}