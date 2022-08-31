using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "spawn_group_formation")]
public class SpawnGroupFormation : ISpawnGroupFormation
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint Id { get; set; }
    
    [Column(Name = "FormationType")]
    public FormationShape FormationType { get; set; }
    
    [Column(Name = "FormationSpread")]
    public float Spread { get; set; }
    
    [Column(Name = "FormationOptions")]
    public int Options { get; set; }
    
    [Column(Name = "PathId")]
    public int PathId { get; set; }
    
    [Column(Name = "MovementType")]
    public MovementType MovementType { get; set; }
    
    [Column(Name = "Comment")]
    public string? Comment { get; set; }
}