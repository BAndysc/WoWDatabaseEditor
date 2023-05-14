using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

public abstract class BaseCreatureAddon : IBaseCreatureAddon
{
    public uint PathId => 0;
    
    [Column(Name = "mount")]
    public uint Mount { get; set; }

    public uint MountCreatureId => 0;
    
    [Column(Name = "stand_state")]
    public byte StandState { get; set; }
    public byte VisFlags => 0;
    public byte AnimTier => 0;
    
    [Column(Name = "sheath_state")]
    public byte Sheath { get; set; }

    public abstract byte PvP { get; set; }

    [Column(Name = "emote")]
    public uint Emote { get; set; }

    public uint VisibilityDistanceType => 0;
    
    [Column(Name = "auras")]
    public string? Auras { get; set; }
    
    [Column(Name = "moveflags")]
    public uint MoveFlags { get; set; }

}

[Table(Name = "creature_addon")]
public class CreatureAddonWrath : BaseCreatureAddon, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }
    
    [Column(Name = "pvp_flags")]
    public override byte PvP { get; set; }
}

[Table(Name = "creature_template_addon")]
public class CreatureTemplateAddonWrath : BaseCreatureAddon, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "pvp_flags")]
    public override byte PvP { get; set; }
}

[Table(Name = "creature_addon")]
public class CreatureAddonTBC : BaseCreatureAddon, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }

    public override byte PvP { get; set; }
}

[Table(Name = "creature_template_addon")]
public class CreatureTemplateAddonTBC : BaseCreatureAddon, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }

    public override byte PvP { get; set; }
}
