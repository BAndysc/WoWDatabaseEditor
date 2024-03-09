
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonSpawnGroupTemplate : ISpawnGroupTemplate
{
    
    
    public uint Id { get; set; }

     
    public string Name { get; set; } = "";

    public SpawnGroupTemplateType Type => SpawnGroupTemplateType.Any;
    
     
    public uint GroupFlags { get; set; }
    
    public uint? TrinityFlags => GroupFlags;
    
    public uint? MangosFlags => null;
}