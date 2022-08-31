using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "spawn_group_spawn")]
public class SpawnGroupSpawn : ISpawnGroupSpawn
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint TemplateId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "Guid")]
    public uint Guid { get; set; }

    public SpawnGroupTemplateType Type => SpawnGroupTemplateType.Any;

    [Column(Name = "SlotId")]
    public int? SlotId { get; set; }
    
    [Column(Name = "Chance")]
    public uint? Chance { get; set; }
}