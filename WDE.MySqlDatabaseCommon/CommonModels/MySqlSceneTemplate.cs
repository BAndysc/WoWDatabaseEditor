using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "scene_template")]
public class MySqlSceneTemplate : ISceneTemplate
{
    [PrimaryKey]
    [Column(Name = "SceneId")]
    public uint SceneId { get; set; }
    
    [Column(Name = "Flags")]
    public uint Flags { get; set; }
    
    [Column(Name = "ScriptPackageID")]
    public uint ScriptPackageId { get; set; }

    [Column(Name = "ScriptName")] 
    public string ScriptName { get; set; } = "";
}