using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>The whole solution-item machinery for <see cref="SpawnGroupsSolutionItem"/> (a KEY-ONLY
/// item, one per group id): session (de)serialization, display name/icon, and SQL regeneration: the
/// group's CURRENT template + membership are loaded from the DB at generate time (the live save
/// already wrote them) and rewritten idempotently - template DELETE+INSERT (always: the export must
/// be standalone-correct on a fresh target DB), membership DELETE-all + bulk INSERT. Template gone
/// from the DB = the rewrite is just the DELETEs.</summary>
[AutoRegister]
public class SpawnGroupsSolutionItemProviders :
    ISolutionItemSerializer<SpawnGroupsSolutionItem>,
    ISolutionItemDeserializer<SpawnGroupsSolutionItem>,
    ISolutionNameProvider<SpawnGroupsSolutionItem>,
    ISolutionItemIconProvider<SpawnGroupsSolutionItem>,
    ISolutionItemSqlProvider<SpawnGroupsSolutionItem>
{
    private const int SerializationType = 140;

    private readonly IDatabaseProvider databaseProvider;
    private readonly IQueryGenerator<ISpawnGroupTemplate> templateGen;
    private readonly IQueryGenerator<ISpawnGroupSpawn> spawnGen;
    private readonly IQueryGenerator<ISpawnGroupFormation> formationGen;
    private readonly IQueryGenerator<ISpawnGroupRandomEntry> randomEntryGen;
    private readonly IQueryGenerator<ISpawnGroupLinkedGroup> linkedGroupGen;
    private readonly IQueryGenerator<ISpawnGroupSquadMember> squadGen;

    public SpawnGroupsSolutionItemProviders(IDatabaseProvider databaseProvider,
        IQueryGenerator<ISpawnGroupTemplate> templateGen,
        IQueryGenerator<ISpawnGroupSpawn> spawnGen,
        IQueryGenerator<ISpawnGroupFormation> formationGen,
        IQueryGenerator<ISpawnGroupRandomEntry> randomEntryGen,
        IQueryGenerator<ISpawnGroupLinkedGroup> linkedGroupGen,
        IQueryGenerator<ISpawnGroupSquadMember> squadGen)
    {
        this.databaseProvider = databaseProvider;
        this.templateGen = templateGen;
        this.spawnGen = spawnGen;
        this.formationGen = formationGen;
        this.randomEntryGen = randomEntryGen;
        this.linkedGroupGen = linkedGroupGen;
        this.squadGen = squadGen;
    }

    public ISmartScriptProjectItem? Serialize(SpawnGroupsSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem
        {
            Type = SerializationType,
            Value = (int)item.GroupId
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        solutionItem = null;
        if (projectItem.Type != SerializationType)
            return false;
        solutionItem = new SpawnGroupsSolutionItem { GroupId = (uint)projectItem.Value };
        return true;
    }

    public string GetName(SpawnGroupsSolutionItem item) => $"Spawn group {item.GroupId}";

    public ImageUri GetIcon(SpawnGroupsSolutionItem icon) => new ImageUri("Icons/document_spawngroup_big.png");

    /// <summary>Key-only row for <c>TryDeleteAll</c> (deletes the group's whole membership).</summary>
    private static ISpawnGroupSpawn GroupKeyRow(uint groupId) => new AbstractSpawnGroupSpawn
    {
        TemplateId = groupId,
        Type = SpawnGroupTemplateType.Creature, // unused by DeleteAll; anything but `Any`
    };

    public async Task<IQuery> GenerateSql(SpawnGroupsSolutionItem item)
    {
        IMultiQuery? multi = null;
        void Add(IQuery? q)
        {
            if (q == null)
                return;
            multi ??= Queries.BeginTransaction(q.Database);
            multi.Add(q);
        }

        // the group's CURRENT state - it may have been renamed, edited or deleted since
        var template = await databaseProvider.GetSpawnGroupTemplateByIdAsync(item.GroupId);
        var members = (await databaseProvider.GetSpawnGroupSpawnsAsync())
            .Where(s => s.TemplateId == item.GroupId)
            .ToList();

        // idempotent rewrite: DELETE everything first, then INSERT the current state
        Add(templateGen.TryDelete(template ?? new AbstractSpawnGroupTemplate { Id = item.GroupId, Name = "" }));
        if (template != null)
            Add(templateGen.TryInsert(template));

        Add(spawnGen.TryDeleteAll(GroupKeyRow(item.GroupId)));
        if (members.Count > 0)
            Add(spawnGen.TryBulkInsert(members));

        // CMaNGOS extras (Try* are no-ops on cores without the tables): same idempotent rewrite
        // shape per side table, always from the DB's current state
        Add(formationGen.TryDeleteAll(new AbstractSpawnGroupFormation { Id = item.GroupId }));
        if ((await databaseProvider.GetSpawnGroupFormation(item.GroupId)) is { } formation)
            Add(formationGen.TryInsert(formation));

        Add(randomEntryGen.TryDeleteAll(new AbstractSpawnGroupRandomEntry { GroupId = item.GroupId }));
        var entries = (await databaseProvider.GetSpawnGroupRandomEntriesAsync())?
            .Where(e => e.GroupId == item.GroupId).ToList();
        if (entries is { Count: > 0 })
            Add(randomEntryGen.TryBulkInsert(entries));

        Add(linkedGroupGen.TryDeleteAll(new AbstractSpawnGroupLinkedGroup { GroupId = item.GroupId }));
        var links = (await databaseProvider.GetSpawnGroupLinkedGroupsAsync())?
            .Where(l => l.GroupId == item.GroupId).ToList();
        if (links is { Count: > 0 })
            Add(linkedGroupGen.TryBulkInsert(links));

        Add(squadGen.TryDeleteAll(new AbstractSpawnGroupSquadMember { GroupId = item.GroupId }));
        var squads = (await databaseProvider.GetSpawnGroupSquadsAsync())?
            .Where(s => s.GroupId == item.GroupId).ToList();
        if (squads is { Count: > 0 })
            Add(squadGen.TryBulkInsert(squads));

        return multi?.Close() ?? Queries.Empty(DataDatabaseType.World);
    }
}
