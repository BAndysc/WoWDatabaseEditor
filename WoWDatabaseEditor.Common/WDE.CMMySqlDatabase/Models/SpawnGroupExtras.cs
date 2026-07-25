using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "spawn_group_entry")]
public class SpawnGroupEntry : ISpawnGroupRandomEntry
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint GroupId { get; set; }

    [PrimaryKey]
    [Column(Name = "Entry")]
    public uint Entry { get; set; }

    [Column(Name = "MinCount")]
    public uint MinCount { get; set; }

    [Column(Name = "MaxCount")]
    public uint MaxCount { get; set; }

    [Column(Name = "Chance")]
    public uint Chance { get; set; }
}

[Table(Name = "spawn_group_linked_group")]
public class SpawnGroupLinkedGroup : ISpawnGroupLinkedGroup
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint GroupId { get; set; }

    [PrimaryKey]
    [Column(Name = "LinkedId")]
    public uint LinkedGroupId { get; set; }
}

[Table(Name = "spawn_group_squad")]
public class SpawnGroupSquad : ISpawnGroupSquadMember
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint GroupId { get; set; }

    [PrimaryKey]
    [Column(Name = "SquadId")]
    public uint SquadId { get; set; }

    [PrimaryKey]
    [Column(Name = "Guid")]
    public uint Guid { get; set; }

    [Column(Name = "Entry")]
    public uint Entry { get; set; }
}
