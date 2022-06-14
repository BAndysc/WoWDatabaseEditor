using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    // replacing areatrigger_template from tc to areatrigger_teleport but it seem cmangos have also areatrigger_tavern to have to be handled
    [Table(Name = "areatrigger_teleport")]
    public class AreaTriggerTemplateWoTLK : IAreaTriggerTemplate
    {
        [PrimaryKey]
        [Column(Name = "id")]
        public uint Id { get; set; }

        // no IsServerSide in cmangos
//         [PrimaryKey]
//         [Column(Name = "IsServerSide")]
        public bool IsServerSide { get; set; } = true;
        
        // [Column(Name = "ScriptName")]
        // public string? ScriptName { get; set; }
    }
}