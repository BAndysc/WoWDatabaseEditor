using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>The whole solution-item machinery for <see cref="PoolsSolutionItem"/> (a KEY-ONLY item,
/// one per pool entry): session (de)serialization, display name/icon, and SQL regeneration: the
/// pool's CURRENT template + members + nesting are loaded from the DB at generate time (the live
/// save already wrote them) and rewritten idempotently - template DELETE+INSERT (always: the export
/// must be standalone-correct on a fresh target DB), members/nesting DELETE-all + bulk INSERT.
/// Template gone from the DB = the rewrite is just the DELETEs.</summary>
[AutoRegister]
public class PoolsSolutionItemProviders :
    ISolutionItemSerializer<PoolsSolutionItem>,
    ISolutionItemDeserializer<PoolsSolutionItem>,
    ISolutionNameProvider<PoolsSolutionItem>,
    ISolutionItemIconProvider<PoolsSolutionItem>,
    ISolutionItemSqlProvider<PoolsSolutionItem>
{
    private const int SerializationType = 145;

    private readonly IDatabaseProvider databaseProvider;
    private readonly IQueryGenerator<IPoolTemplate> templateGen;
    private readonly IQueryGenerator<IPoolCreatureMember> creatureGen;
    private readonly IQueryGenerator<IPoolGameObjectMember> gameObjectGen;
    private readonly IQueryGenerator<IPoolCreatureEntryMember> creatureEntryGen;
    private readonly IQueryGenerator<IPoolGameObjectEntryMember> gameObjectEntryGen;
    private readonly IQueryGenerator<IPoolNesting> nestingGen;

    public PoolsSolutionItemProviders(IDatabaseProvider databaseProvider,
        IQueryGenerator<IPoolTemplate> templateGen,
        IQueryGenerator<IPoolCreatureMember> creatureGen,
        IQueryGenerator<IPoolGameObjectMember> gameObjectGen,
        IQueryGenerator<IPoolCreatureEntryMember> creatureEntryGen,
        IQueryGenerator<IPoolGameObjectEntryMember> gameObjectEntryGen,
        IQueryGenerator<IPoolNesting> nestingGen)
    {
        this.databaseProvider = databaseProvider;
        this.templateGen = templateGen;
        this.creatureGen = creatureGen;
        this.gameObjectGen = gameObjectGen;
        this.creatureEntryGen = creatureEntryGen;
        this.gameObjectEntryGen = gameObjectEntryGen;
        this.nestingGen = nestingGen;
    }

    public ISmartScriptProjectItem? Serialize(PoolsSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem
        {
            Type = SerializationType,
            Value = (int)item.PoolId
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        solutionItem = null;
        if (projectItem.Type != SerializationType)
            return false;
        solutionItem = new PoolsSolutionItem { PoolId = (uint)projectItem.Value };
        return true;
    }

    public string GetName(PoolsSolutionItem item) => $"Spawn pool {item.PoolId}";

    public ImageUri GetIcon(PoolsSolutionItem icon) => new ImageUri("Icons/document_pool_big.png");

    public async Task<IQuery> GenerateSql(PoolsSolutionItem item)
    {
        IMultiQuery? multi = null;
        void Add(IQuery? q)
        {
            if (q == null)
                return;
            multi ??= Queries.BeginTransaction(q.Database);
            multi.Add(q);
        }

        var id = item.PoolId;

        // the pool's CURRENT state - it may have been renamed, edited or deleted since
        var template = await databaseProvider.GetPoolTemplateByIdAsync(id);

        // idempotent rewrite: DELETE everything first, then INSERT the current state
        Add(templateGen.TryDelete(template ?? new AbstractPoolTemplate { Entry = id }));
        if (template != null)
            Add(templateGen.TryInsert(template));

        Add(creatureGen.TryDeleteAll(new AbstractPoolCreatureMember { PoolEntry = id }));
        var creatures = (await databaseProvider.GetPoolCreaturesAsync())?
            .Where(c => c.PoolEntry == id).ToList();
        if (creatures is { Count: > 0 })
            Add(creatureGen.TryBulkInsert(creatures));

        Add(gameObjectGen.TryDeleteAll(new AbstractPoolGameObjectMember { PoolEntry = id }));
        var gameObjects = (await databaseProvider.GetPoolGameObjectsAsync())?
            .Where(g => g.PoolEntry == id).ToList();
        if (gameObjects is { Count: > 0 })
            Add(gameObjectGen.TryBulkInsert(gameObjects));

        Add(creatureEntryGen.TryDeleteAll(new AbstractPoolCreatureEntryMember { PoolEntry = id }));
        var creatureEntries = (await databaseProvider.GetPoolCreatureEntryPoolsAsync())?
            .Where(c => c.PoolEntry == id).ToList();
        if (creatureEntries is { Count: > 0 })
            Add(creatureEntryGen.TryBulkInsert(creatureEntries));

        Add(gameObjectEntryGen.TryDeleteAll(new AbstractPoolGameObjectEntryMember { PoolEntry = id }));
        var gameObjectEntries = (await databaseProvider.GetPoolGameObjectEntryPoolsAsync())?
            .Where(g => g.PoolEntry == id).ToList();
        if (gameObjectEntries is { Count: > 0 })
            Add(gameObjectEntryGen.TryBulkInsert(gameObjectEntries));

        // nesting rows in both directions (the pool's own mother link + its children's links);
        // DeleteAll already covers pool_id = X OR mother_pool = X
        Add(nestingGen.TryDeleteAll(new AbstractPoolNesting { PoolId = id }));
        var nestings = (await databaseProvider.GetPoolNestingsAsync())?
            .Where(n => n.PoolId == id || n.MotherPool == id).ToList();
        if (nestings is { Count: > 0 })
            Add(nestingGen.TryBulkInsert(nestings));

        return multi?.Close() ?? Queries.Empty(DataDatabaseType.World);
    }
}
