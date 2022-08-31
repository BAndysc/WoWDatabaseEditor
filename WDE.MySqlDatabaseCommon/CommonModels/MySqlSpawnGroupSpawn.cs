using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "spawn_group")]
public class MySqlSpawnGroupSpawn : ISpawnGroupSpawn
{
    [PrimaryKey]
    [Column(Name = "groupId")]
    public uint TemplateId { get; set; }
    
    [PrimaryKey]
    [Column(Name = "spawnId")]
    public uint Guid { get; set; }
    
    [PrimaryKey]
    [Column(Name = "spawnType")]
    public SpawnGroupTemplateType Type { get; set; }

    public int? SlotId => null;
    
    public uint? Chance => null;
}