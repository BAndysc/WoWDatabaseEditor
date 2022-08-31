using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "spawn_group")]
public class SpawnGroup : ISpawnGroupTemplate
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint Id { get; set; }

    [Column(Name = "Name")] 
    public string Name { get; set; } = "";

    [Column(Name = "Type")] 
    public SpawnGroupTemplateType Type { get; set; }
    
    [Column(Name = "Flags")] 
    public uint Flags { get; set; }
    
    [Column(Name = "MaxCount")]
    public int MaxCount { get; set; }
    
    [Column(Name = "WorldState")]
    public int WorldState { get; set; }
    
    public uint? TrinityFlags => null;
    
    public uint? MangosFlags => Flags;
}