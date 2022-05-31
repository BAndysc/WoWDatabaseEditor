using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "creature_model_info")]
public abstract class MySqlCreatureModelInfoBase : ICreatureModelInfo
{
    [PrimaryKey]
    [Column(Name = "DisplayID")]
    public uint DisplayId { get; set; }
    
    [Column(Name = "BoundingRadius")]
    public float BoundingRadius { get; set; }
    
    [Column(Name = "CombatReach")]
    public float CombatReach { get; set; }

    public abstract uint Gender { get; set; }
}

[Table(Name = "creature_model_info")]
public class MySqlCreatureModelInfoShadowlands : MySqlCreatureModelInfoBase
{
    public override uint Gender { get; set; }
}

[Table(Name = "creature_model_info")]
public class MySqlCreatureModelInfo : MySqlCreatureModelInfoBase
{
    [Column(Name = "Gender")]
    public override uint Gender { get; set; }
}