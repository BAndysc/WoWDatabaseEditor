using WDE.Common.Database;
using WDE.Module.Attributes;
using WDE.QueryGenerators.Base;
using WDE.SqlQueryGenerator;

namespace WDE.QueryGenerators.Generators.SafeLocs;

// CMaNGOS world safe locs (graveyards) + spell target positions. Same capability pattern as the
// spawn-group side tables: no provider on other cores -> IQueryGenerator<T>.TableName is null and
// the 3D editors hide their tools. Saves are idempotent per-key rewrites (delete + insert).

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosWorldSafeLocQueryProvider : BaseInsertQueryProvider<IWorldSafeLoc>,
    IDeleteQueryProvider<IWorldSafeLoc>
{
    protected override object Convert(IWorldSafeLoc loc)
    {
        return new
        {
            id = loc.Id,
            map = loc.Map,
            x = loc.X,
            y = loc.Y,
            z = loc.Z,
            o = loc.O,
            name = loc.Name,
        };
    }

    public IQuery Delete(IWorldSafeLoc t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.Id)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("world_safe_locs");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosGraveyardLinkQueryProvider : BaseInsertQueryProvider<IGraveyardLink>,
    IDeleteQueryProvider<IGraveyardLink>, IDeleteAllQueryProvider<IGraveyardLink>
{
    protected override object Convert(IGraveyardLink link)
    {
        return new
        {
            id = link.SafeLocId,
            ghost_loc = link.GhostLoc,
            link_kind = (uint)link.LinkKind,
            faction = link.Faction,
        };
    }

    public IQuery Delete(IGraveyardLink t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.SafeLocId &&
                          row.Column<uint>("ghost_loc") == t.GhostLoc &&
                          row.Column<uint>("link_kind") == (uint)t.LinkKind)
            .Delete();
    }

    public IQuery DeleteAll(IGraveyardLink t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.SafeLocId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("game_graveyard_zone");
}

[AutoRegister]
[RequiresCore("CMaNGOS-WoTLK", "CMaNGOS-TBC", "CMaNGOS-Classic")]
internal class MangosSpellTargetPositionQueryProvider : BaseInsertQueryProvider<ISpellTargetPosition>,
    IDeleteQueryProvider<ISpellTargetPosition>
{
    protected override object Convert(ISpellTargetPosition position)
    {
        return new
        {
            id = position.SpellId,
            target_map = position.Map,
            target_position_x = position.X,
            target_position_y = position.Y,
            target_position_z = position.Z,
            target_orientation = position.O,
        };
    }

    public IQuery Delete(ISpellTargetPosition t)
    {
        return Queries.Table(TableName)
            .Where(row => row.Column<uint>("id") == t.SpellId)
            .Delete();
    }

    public override DatabaseTable TableName => DatabaseTable.WorldTable("spell_target_position");
}
