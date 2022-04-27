using LinqToDB.Mapping;
using WDE.Common.DBC;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "item_template")]
public class MySqlItemTemplate : IItem
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint Entry { get; set; }

    [Column(Name = "name")] 
    public string Name { get; set; } = "";
    
    [Column(Name = "displayid")] 
    public uint DisplayId { get; set; }
}