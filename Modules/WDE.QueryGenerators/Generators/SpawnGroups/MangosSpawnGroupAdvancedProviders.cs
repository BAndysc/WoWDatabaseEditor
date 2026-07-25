using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SpawnGroups;

// The CMaNGOS-only spawn-group side tables. Every provider follows the editor's idempotent
// "rewrite the group" save shape: DeleteAll (all of the group's rows) + BulkInsert the current
// state. On cores without these tables no provider registers, IQueryGenerator<T>.TableName is
// null and the editor hides the feature (the same capability pattern the waypoint editor uses).

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupFormationQueryProvider : BaseInsertQueryProvider<ISpawnGroupFormation>,
    IDeleteQueryProvider<ISpawnGroupFormation>, IDeleteAllQueryProvider<ISpawnGroupFormation>
{
    protected override object Convert(ISpawnGroupFormation formation)
    {
        return new
        {
            Id = formation.Id,
            FormationType = (int)formation.FormationType,
            FormationSpread = formation.Spread,
            FormationOptions = formation.Options,
            PathId = formation.PathId,
            MovementType = (int)formation.MovementType,
            Comment = formation.Comment,
        };
    }

    public IQuery Delete(ISpawnGroupFormation t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.Id)
            .Delete();
    }

    // spawn_group_formation is keyed by group id alone - Delete and DeleteAll coincide
    public IQuery DeleteAll(ISpawnGroupFormation t) => Delete(t);

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_formation");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupRandomEntryQueryProvider : BaseInsertQueryProvider<ISpawnGroupRandomEntry>,
    IDeleteQueryProvider<ISpawnGroupRandomEntry>, IDeleteAllQueryProvider<ISpawnGroupRandomEntry>
{
    protected override object Convert(ISpawnGroupRandomEntry entry)
    {
        return new
        {
            Id = entry.GroupId,
            Entry = entry.Entry,
            MinCount = entry.MinCount,
            MaxCount = entry.MaxCount,
            Chance = entry.Chance,
        };
    }

    public IQuery Delete(ISpawnGroupRandomEntry t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.GroupId && row.Column<uint>("Entry") == t.Entry)
            .Delete();
    }

    public IQuery DeleteAll(ISpawnGroupRandomEntry t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.GroupId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_entry");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupLinkedGroupQueryProvider : BaseInsertQueryProvider<ISpawnGroupLinkedGroup>,
    IDeleteQueryProvider<ISpawnGroupLinkedGroup>, IDeleteAllQueryProvider<ISpawnGroupLinkedGroup>
{
    protected override object Convert(ISpawnGroupLinkedGroup link)
    {
        return new
        {
            Id = link.GroupId,
            LinkedId = link.LinkedGroupId,
        };
    }

    public IQuery Delete(ISpawnGroupLinkedGroup t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.GroupId && row.Column<uint>("LinkedId") == t.LinkedGroupId)
            .Delete();
    }

    public IQuery DeleteAll(ISpawnGroupLinkedGroup t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.GroupId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_linked_group");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpawnGroupSquadQueryProvider : BaseInsertQueryProvider<ISpawnGroupSquadMember>,
    IDeleteQueryProvider<ISpawnGroupSquadMember>, IDeleteAllQueryProvider<ISpawnGroupSquadMember>
{
    protected override object Convert(ISpawnGroupSquadMember member)
    {
        return new
        {
            Id = member.GroupId,
            SquadId = member.SquadId,
            Guid = member.Guid,
            Entry = member.Entry,
        };
    }

    public IQuery Delete(ISpawnGroupSquadMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.GroupId &&
                          row.Column<uint>("SquadId") == t.SquadId &&
                          row.Column<uint>("Guid") == t.Guid)
            .Delete();
    }

    public IQuery DeleteAll(ISpawnGroupSquadMember t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("Id") == t.GroupId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spawn_group_squad");
}
