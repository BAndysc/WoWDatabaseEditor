using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "world_safe_locs")]
public class WorldSafeLoc : IWorldSafeLoc
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint Id { get; set; }

    [Column(Name = "map")]
    public uint Map { get; set; }

    [Column(Name = "x")]
    public float X { get; set; }

    [Column(Name = "y")]
    public float Y { get; set; }

    [Column(Name = "z")]
    public float Z { get; set; }

    [Column(Name = "o")]
    public float O { get; set; }

    [Column(Name = "name")]
    public string Name { get; set; } = "";
}

[Table(Name = "game_graveyard_zone")]
public class GraveyardLink : IGraveyardLink
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint SafeLocId { get; set; }

    [PrimaryKey]
    [Column(Name = "ghost_loc")]
    public uint GhostLoc { get; set; }

    [PrimaryKey]
    [Column(Name = "link_kind")]
    public GraveyardLinkKind LinkKind { get; set; }

    [Column(Name = "faction")]
    public uint Faction { get; set; }
}

[Table(Name = "spell_target_position")]
public class SpellTargetPosition : ISpellTargetPosition
{
    [PrimaryKey]
    [Column(Name = "id")]
    public uint SpellId { get; set; }

    [Column(Name = "target_map")]
    public uint Map { get; set; }

    [Column(Name = "target_position_x")]
    public float X { get; set; }

    [Column(Name = "target_position_y")]
    public float Y { get; set; }

    [Column(Name = "target_position_z")]
    public float Z { get; set; }

    [Column(Name = "target_orientation")]
    public float O { get; set; }
}
