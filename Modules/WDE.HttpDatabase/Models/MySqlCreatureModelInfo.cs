
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public abstract class JsonCreatureModelInfoBase : ICreatureModelInfo
{
    
    
    public uint DisplayId { get; set; }
    
    
    public float BoundingRadius { get; set; }
    
    
    public float CombatReach { get; set; }

    public abstract uint Gender { get; set; }
}


public class JsonCreatureModelInfoShadowlands : JsonCreatureModelInfoBase
{
    public override uint Gender { get; set; }
}


public class JsonCreatureModelInfo : JsonCreatureModelInfoBase
{
    
    public override uint Gender { get; set; }
}