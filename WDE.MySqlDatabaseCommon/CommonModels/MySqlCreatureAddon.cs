using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

public abstract class MySqlBaseCreatureAddon : IBaseCreatureAddon
{
    public abstract uint PathId { get; set; }
    
    [Column(Name = "mount")]
    public uint Mount { get; set; }

    public abstract uint MountCreatureId { get; set; }

    [Column(Name = "bytes1")]
    public uint Bytes1 { get; set; }

    [Column(Name = "bytes2")]
    public uint Bytes2 { get; set; }

    [Column(Name = "emote")]
    public uint Emote { get; set; }

    [Column(Name = "visibilityDistanceType")]
    public uint VisibilityDistanceType { get; set; }
    
    [Column(Name = "auras")]
    public string? Auras { get; set; }

    public byte Sheath => (byte)(Bytes2 & 0xFF);
    
    public byte PvP => (byte)((Bytes2 >> 8) & 0xFF);
}

[Table(Name = "creature_addon")]
public class MySqlCreatureAddon : MySqlBaseCreatureAddon, ICreatureAddon
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
public class MySqlCreatureTemplateAddon : MySqlBaseCreatureAddon, ICreatureTemplateAddon
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
public class MySqlCreatureAddonCata : MySqlBaseCreatureAddon, ICreatureAddon
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
public class MySqlCreatureTemplateAddonCata : MySqlBaseCreatureAddon, ICreatureTemplateAddon
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
public class MySqlCreatureAddonAC: MySqlBaseCreatureAddon, ICreatureAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "guid")]
    public uint Guid { get; set; }
    
    [Column(Name = "path_id")]
    public override uint PathId { get; set; }

    public override uint MountCreatureId { get; set; }
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
}