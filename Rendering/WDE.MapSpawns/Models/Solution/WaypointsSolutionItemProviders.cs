using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.MapSpawns.Models.Waypoints;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>The whole solution-item machinery for <see cref="WaypointsSolutionItem"/> (one item per
/// path, keyed by source table + key): session (de)serialization (compact "source:key" - the item is
/// only a path REFERENCE), display name/icon, and SQL regeneration: the path's current rows are loaded
/// from the DB at generate time and rewritten via the shared <see cref="WaypointSaveSql"/> builder.</summary>
[AutoRegister]
public class WaypointsSolutionItemProviders :
    ISolutionItemSerializer<WaypointsSolutionItem>,
    ISolutionItemDeserializer<WaypointsSolutionItem>,
    ISolutionNameProvider<WaypointsSolutionItem>,
    ISolutionItemIconProvider<WaypointsSolutionItem>,
    ISolutionItemSqlProvider<WaypointsSolutionItem>
{
    private const int SerializationType = 142;

    private readonly IDatabaseProvider databaseProvider;
    private readonly IQueryGenerator<IWaypointData> waypointDataGen;
    private readonly IQueryGenerator<ISmartScriptWaypoint> smartScriptGen;
    private readonly IQueryGenerator<IScriptWaypoint> scriptWaypointGen;
    private readonly IQueryGenerator<IMangosWaypoint> mangosWaypointGen;
    private readonly IQueryGenerator<IMangosCreatureMovement> mangosMovementGen;
    private readonly IQueryGenerator<IMangosCreatureMovementTemplate> mangosMovementTemplateGen;
    private readonly IQueryGenerator<IWaypointPathHeader> pathHeaderGen;
    private readonly IQueryGenerator<IMangosWaypointsPathName> pathNameGen;
    private readonly IWaypointSchemaInfoProvider? schemaInfo;

    public WaypointsSolutionItemProviders(IDatabaseProvider databaseProvider,
        IQueryGenerator<IWaypointData> waypointDataGen,
        IQueryGenerator<ISmartScriptWaypoint> smartScriptGen,
        IQueryGenerator<IScriptWaypoint> scriptWaypointGen,
        IQueryGenerator<IMangosWaypoint> mangosWaypointGen,
        IQueryGenerator<IMangosCreatureMovement> mangosMovementGen,
        IQueryGenerator<IMangosCreatureMovementTemplate> mangosMovementTemplateGen,
        IQueryGenerator<IWaypointPathHeader> pathHeaderGen,
        IQueryGenerator<IMangosWaypointsPathName> pathNameGen,
        IEnumerable<IWaypointSchemaInfoProvider> schemaInfoProviders)
    {
        this.databaseProvider = databaseProvider;
        this.waypointDataGen = waypointDataGen;
        this.smartScriptGen = smartScriptGen;
        this.scriptWaypointGen = scriptWaypointGen;
        this.mangosWaypointGen = mangosWaypointGen;
        this.mangosMovementGen = mangosMovementGen;
        this.mangosMovementTemplateGen = mangosMovementTemplateGen;
        this.pathHeaderGen = pathHeaderGen;
        this.pathNameGen = pathNameGen;
        schemaInfo = schemaInfoProviders.FirstOrDefault(); // [RequiresCore]: at most one per active core
    }

    public ISmartScriptProjectItem? Serialize(WaypointsSolutionItem item, bool forMostRecentlyUsed)
    {
        // "source:key" for single-key paths (unchanged), "source:key:key2" for compound-keyed ones
        return new AbstractSmartScriptProjectItem
        {
            Type = SerializationType,
            StringValue = item.Key2 == 0 ? $"{item.Source}:{item.Key}" : $"{item.Source}:{item.Key}:{item.Key2}"
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        solutionItem = null;
        if (projectItem.Type != SerializationType || projectItem.StringValue == null)
            return false;

        // accept both the legacy 2-part "source:key" and the compound 3-part "source:key:key2"
        var parts = projectItem.StringValue.Split(':');
        if (parts.Length is not (2 or 3) || !int.TryParse(parts[0], out var source) || !uint.TryParse(parts[1], out var key))
            return false;
        uint key2 = 0;
        if (parts.Length == 3 && !uint.TryParse(parts[2], out key2))
            return false;

        solutionItem = new WaypointsSolutionItem { Source = source, Key = key, Key2 = key2 };
        return true;
    }

    public string GetName(WaypointsSolutionItem item)
    {
        var source = (WaypointSource)item.Source;
        var keyLabel = item.Key2 == 0 ? $"#{item.Key}" : $"entry {item.Key} / path {item.Key2}";
        return $"Waypoints {source.ToName(schemaInfo)} {keyLabel}";
    }

    public ImageUri GetIcon(WaypointsSolutionItem icon) => new ImageUri("Icons/document_waypoints_big.png");

    public async Task<IQuery> GenerateSql(WaypointsSolutionItem item)
    {
        var source = (WaypointSource)item.Source;
        var points = await WaypointDbLoader.LoadPoints(databaseProvider, source, item.Key, item.Key2)
                     ?? new List<UniversalWaypoint>(); // path deleted since -> rewrite = just the DELETE

        // path-level rows are re-read at generate time exactly like the points, so the exported
        // query rewrites them too (skipped when the path was removed - DeleteAll drops them)
        IWaypointPathHeader? header = null;
        IMangosWaypointsPathName? pathName = null;
        if (points.Count > 0)
        {
            if (source == WaypointSource.TrinityWaypointData)
                header = await databaseProvider.GetWaypointPathHeader(item.Key);
            if (source == WaypointSource.MangosWaypointPath)
                pathName = await databaseProvider.GetMangosPathName(item.Key);
        }

        var query = WaypointSaveSql.Build(source, item.Key, item.Key2, points,
            waypointDataGen, smartScriptGen, scriptWaypointGen, mangosWaypointGen, mangosMovementGen, mangosMovementTemplateGen,
            header, pathHeaderGen, pathName, pathNameGen);
        // null = no provider for this source on the active core
        return query ?? Queries.Empty(DataDatabaseType.World);
    }
}
