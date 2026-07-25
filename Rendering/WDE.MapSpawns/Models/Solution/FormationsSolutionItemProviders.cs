using System.Threading.Tasks;
using WDE.Common;
using WDE.Common.Database;
using WDE.Common.Solution;
using WDE.Common.Types;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.MapSpawns.Models.Solution;

/// <summary>The whole solution-item machinery for <see cref="FormationsSolutionItem"/> (a KEY-ONLY
/// item, one per leader guid): session (de)serialization, display name/icon, and SQL regeneration:
/// the group's CURRENT creature_formations rows are loaded from the DB at generate time (the live
/// save already wrote them) and rewritten idempotently (DELETE-by-leader + bulk INSERT).</summary>
[AutoRegister]
public class FormationsSolutionItemProviders :
    ISolutionItemSerializer<FormationsSolutionItem>,
    ISolutionItemDeserializer<FormationsSolutionItem>,
    ISolutionNameProvider<FormationsSolutionItem>,
    ISolutionItemIconProvider<FormationsSolutionItem>,
    ISolutionItemSqlProvider<FormationsSolutionItem>
{
    private const int SerializationType = 141;

    private readonly IDatabaseProvider databaseProvider;
    private readonly IQueryGenerator<ICreatureFormation> formationGen;

    public FormationsSolutionItemProviders(IDatabaseProvider databaseProvider,
        IQueryGenerator<ICreatureFormation> formationGen)
    {
        this.databaseProvider = databaseProvider;
        this.formationGen = formationGen;
    }

    public ISmartScriptProjectItem? Serialize(FormationsSolutionItem item, bool forMostRecentlyUsed)
    {
        return new AbstractSmartScriptProjectItem
        {
            Type = SerializationType,
            Value = (int)item.LeaderGuid
        };
    }

    public bool TryDeserialize(ISmartScriptProjectItem projectItem, out ISolutionItem? solutionItem)
    {
        solutionItem = null;
        if (projectItem.Type != SerializationType)
            return false;
        solutionItem = new FormationsSolutionItem { LeaderGuid = (uint)projectItem.Value };
        return true;
    }

    public string GetName(FormationsSolutionItem item) => $"Formation of leader {item.LeaderGuid}";

    public ImageUri GetIcon(FormationsSolutionItem icon) => new ImageUri("Icons/document_creature_summon_groups_big.png");

    private sealed class LeaderKeyRow : ICreatureFormation
    {
        public uint LeaderGuid { get; init; }
        public uint MemberGuid { get; init; }
        public float Dist => 0;
        public float Angle => 0;
        public uint GroupAi => 0;
        public uint Point1 => 0;
        public uint Point2 => 0;
    }

    public async Task<IQuery> GenerateSql(FormationsSolutionItem item)
    {
        var delete = formationGen.TryDelete(new LeaderKeyRow { LeaderGuid = item.LeaderGuid, MemberGuid = item.LeaderGuid });
        if (delete == null) // no creature_formations provider for the active core
            return Queries.Empty(DataDatabaseType.World);

        var multi = Queries.BeginTransaction(delete.Database);
        multi.Add(delete);

        // the group's CURRENT rows - the formation may have been edited or deleted since (no rows =
        // the rewrite is just the DELETE)
        var members = (await databaseProvider.GetCreatureFormations())
            .Where(f => f.LeaderGuid == item.LeaderGuid)
            .ToList();
        if (members.Count > 0)
        {
            var insert = formationGen.TryBulkInsert(members);
            if (insert != null)
                multi.Add(insert);
        }

        return multi.Close();
    }
}
