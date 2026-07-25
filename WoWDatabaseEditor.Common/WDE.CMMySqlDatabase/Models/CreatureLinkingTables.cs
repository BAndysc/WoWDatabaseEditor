using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "creature_linking")]
public class CreatureLinking : ICreatureLinking
{
    [PrimaryKey]
    [Column(Name = "guid")]
    public uint Guid { get; set; }

    [Column(Name = "master_guid")]
    public uint MasterGuid { get; set; }

    [Column(Name = "flag")]
    public uint Flag { get; set; }
}

[Table(Name = "creature_linking_template")]
public class CreatureLinkingTemplate : ICreatureLinkingTemplate
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint Entry { get; set; }

    [PrimaryKey]
    [Column(Name = "map")]
    public uint Map { get; set; }

    [Column(Name = "master_entry")]
    public uint MasterEntry { get; set; }

    [Column(Name = "flag")]
    public uint Flag { get; set; }

    [Column(Name = "search_range")]
    public uint SearchRange { get; set; }
}
