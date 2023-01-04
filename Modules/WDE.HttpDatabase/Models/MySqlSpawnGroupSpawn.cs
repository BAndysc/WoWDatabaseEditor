
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonSpawnGroupSpawn : ISpawnGroupSpawn
{
    
    
    public uint TemplateId { get; set; }
    
    
    
    public uint Guid { get; set; }
    
    
    
    public SpawnGroupTemplateType Type { get; set; }

    public int? SlotId => null;
    
    public uint? Chance => null;
}