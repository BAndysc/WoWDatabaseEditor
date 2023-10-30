using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "reference_loot_template_names")]
public class ReferenceLootTemplateName : ILootTemplateName
{
    [PrimaryKey]
    [Column(Name = "entry")]
    public uint Entry { get; set; }
    
    [Column(Name = "name")]
    public string Name { get; set; } = "";

    public bool DontLoadRecursively => false;
}