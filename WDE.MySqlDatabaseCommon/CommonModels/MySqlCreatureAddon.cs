using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

public abstract class MySqlBaseCreatureAddon : IBaseCreatureAddon
{
    public abstract uint PathId { get; set; }
    
    [Column(Name = "mount")]
    public uint Mount { get; set; }

    public abstract uint MountCreatureId { get; set; }

    public abstract uint Bytes1 { get; }
    public abstract byte Sheath { get; }
    public abstract byte PvP { get; }

    [Column(Name = "emote")]
    public uint Emote { get; set; }

    [Column(Name = "visibilityDistanceType")]
    public uint VisibilityDistanceType { get; set; }
    
    [Column(Name = "auras")]
    public string? Auras { get; set; }
}

public abstract class MySqlBaseCreatureAddonTrinity : MySqlBaseCreatureAddon
{
    public override uint Bytes1 => StandState;
    public override byte Sheath => sheath;
    public override byte PvP => pvP;
    
    [Column(Name = "StandState")]
    public byte StandState { get; set; }
    
    [Column(Name = "SheathState")]
    public byte sheath { get; set; }

    [Column(Name = "PvPFlags")]
    public byte pvP { get; set; }
}

[Table(Name = "creature_addon")]
public class MySqlCreatureAddonWrath : MySqlBaseCreatureAddonTrinity, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }
    
    [Column(Name = "MountCreatureID")]
    public override uint MountCreatureId { get; set; }
}

[Table(Name = "creature_template_addon")]
public class MySqlCreatureTemplateAddon : MySqlBaseCreatureAddonTrinity, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }

    [Column(Name = "MountCreatureID")]
    public override uint MountCreatureId { get; set; }
}

[Table(Name = "creature_addon")]
public class MySqlCreatureAddonCata : MySqlBaseCreatureAddonTrinity, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }
    
    [Column(Name = "waypointPathId")]
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
}

[Table(Name = "creature_template_addon")]
public class MySqlCreatureTemplateAddonCata : MySqlBaseCreatureAddonTrinity, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "waypointPathId")]
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
}

[Table(Name = "creature_addon")]
public class MySqlCreatureAddonMaster: MySqlBaseCreatureAddon, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }

    [Column(Name = "MountCreatureID")]
    public override uint MountCreatureId { get; set; }

    public override uint Bytes1 => bytes1;
    public override byte Sheath => (byte)(bytes2 & 0xFF);
    public override byte PvP => (byte)((bytes2 >> 8) & 0xFF);

    [Column(Name = "bytes1")]
    public uint bytes1 { get; set; }
    
    [Column(Name = "bytes2")]
    public uint bytes2 { get; set; }
}

[Table(Name = "creature_template_addon")]
public class MySqlCreatureTemplateAddonMaster : MySqlBaseCreatureAddon, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }

    [Column(Name = "MountCreatureID")]
    public override uint MountCreatureId { get; set; }
    
    public override uint Bytes1 => bytes1;
    public override byte Sheath => (byte)(bytes2 & 0xFF);
    public override byte PvP => (byte)((bytes2 >> 8) & 0xFF);

    [Column(Name = "bytes1")]
    public uint bytes1 { get; set; }
    
    [Column(Name = "bytes2")]
    public uint bytes2 { get; set; }
}

[Table(Name = "creature_addon")]
public class MySqlCreatureAddonAC: MySqlBaseCreatureAddon, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }

    public override uint Bytes1 => bytes1;
    public override byte Sheath => (byte)(bytes2 & 0xFF);
    public override byte PvP => (byte)((bytes2 >> 8) & 0xFF);

    [Column(Name = "bytes1")]
    public uint bytes1 { get; set; }
    
    [Column(Name = "bytes2")]
    public uint bytes2 { get; set; }
}

[Table(Name = "creature_template_addon")]
public class MySqlCreatureTemplateAddonAC : MySqlBaseCreatureAddon, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
    
    public override uint Bytes1 => bytes1;
    public override byte Sheath => (byte)(bytes2 & 0xFF);
    public override byte PvP => (byte)((bytes2 >> 8) & 0xFF);

    [Column(Name = "bytes1")]
    public uint bytes1 { get; set; }
    
    [Column(Name = "bytes2")]
    public uint bytes2 { get; set; }
}