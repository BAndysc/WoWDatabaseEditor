
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonSceneTemplate : ISceneTemplate
{
    
    
    public uint SceneId { get; set; }
    
    
    public uint Flags { get; set; }
    
    
    public uint ScriptPackageId { get; set; }

     
    public string ScriptName { get; set; } = "";
}