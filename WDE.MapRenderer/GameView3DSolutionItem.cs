using System.Collections.ObjectModel;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.MapRenderer;

/// <summary>A phantom, non-persistable solution item representing a live 3D view document's pending
/// changes. It exists only so the global "Generate query" / "Copy SQL" buttons (which work on the
/// active document's SolutionItem) can reach the 3D editors' pending save SQL - the generated SQL
/// is exactly what the document's Save would execute.</summary>
public class GameView3DSolutionItem : ISolutionItem
{
    private readonly Func<Task<string>> queryGetter;

    public GameView3DSolutionItem(Func<Task<string>> queryGetter) => this.queryGetter = queryGetter;

    public bool IsContainer => false;
    public ObservableCollection<ISolutionItem>? Items => null;
    public string? ExtraId => null;
    public bool IsExportable => false;
    public ISolutionItem Clone() => this;

    public Task<string> BuildPendingSql() => queryGetter();
}

[AutoRegister]
public class GameView3DSolutionItemProviders :
    ISolutionNameProvider<GameView3DSolutionItem>,
    ISolutionItemSqlProvider<GameView3DSolutionItem>
{
    public string GetName(GameView3DSolutionItem item) => "3D view pending changes";

    public async Task<IQuery> GenerateSql(GameView3DSolutionItem item) =>
        Queries.Raw(DataDatabaseType.World, await item.BuildPendingSql());
}
