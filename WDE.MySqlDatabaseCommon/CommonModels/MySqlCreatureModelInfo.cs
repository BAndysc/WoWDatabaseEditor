using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "creature_model_info")]
public class MySqlCreatureModelInfo : ICreatureModelInfo
{
    [PrimaryKey]
    [Column(Name = "DisplayID")]
    public uint DisplayId { get; set; }
    
    [Column(Name = "BoundingRadius")]
    public float BoundingRadius { get; set; }
    
    [Column(Name = "CombatReach")]
    public float CombatReach { get; set; }
    
    [Column(Name = "Gender")]
    public uint Gender { get; set; }
}