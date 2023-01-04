
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonCreatureTemplateDifficulty : ICreatureTemplateDifficulty
{
    
    
    public uint Entry { get; set; }
    
    
    
    public uint DifficultyId { get; set; }

    
    public uint LootId { get; set; }
    
    
    public uint SkinningLootId { get; set; }
    
    
    public uint PickpocketLootId { get; set; }
}