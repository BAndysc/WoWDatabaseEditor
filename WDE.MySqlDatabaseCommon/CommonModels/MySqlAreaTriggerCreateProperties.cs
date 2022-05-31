using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "areatrigger_create_properties")]
public class MySqlAreaTriggerCreateProperties : IAreaTriggerCreateProperties
{
    [PrimaryKey]
    [Column(Name = "Id")]
    public uint Id { get; set; }

    [Column(Name = "AreaTriggerId")]
    public uint AreaTriggerId { get; set; }

    [Column(Name = "ScriptName")]
    public string ScriptName { get; set; } = "";
}