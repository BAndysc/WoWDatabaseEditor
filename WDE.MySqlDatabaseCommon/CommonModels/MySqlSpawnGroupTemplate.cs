using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "spawn_group_template")]
public class MySqlSpawnGroupTemplate : ISpawnGroupTemplate
{
    [PrimaryKey]
    [Column(Name = "groupId")]
    public uint Id { get; set; }

    [Column(Name = "groupName")] 
    public string Name { get; set; } = "";

    public SpawnGroupTemplateType Type => SpawnGroupTemplateType.Any;
    
    [Column(Name = "groupFlags")] 
    public uint GroupFlags { get; set; }
    
    public uint? TrinityFlags => GroupFlags;
    
    public uint? MangosFlags => null;
}