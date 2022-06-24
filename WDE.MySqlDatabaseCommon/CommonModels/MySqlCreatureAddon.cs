using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

public class MySqlBaseCreatureAddon : IBaseCreatureAddon
{
    [Column(Name = "path_id")]
    public uint PathId { get; set; }
    
    [Column(Name = "mount")]
    public uint Mount { get; set; }
    
    [Column(Name = "MountCreatureID")]
    public uint MountCreatureId { get; set; }
    
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
}

[Table(Name = "creature_template_addon")]
public class MySqlCreatureTemplateAddon : MySqlBaseCreatureAddon, ICreatureTemplateAddon
{
    [PrimaryKey]
    [Identity]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
}