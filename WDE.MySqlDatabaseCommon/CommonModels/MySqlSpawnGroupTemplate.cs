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
    
    [Column(Name = "groupFlags")] 
    public uint Flags { get; set; }
}