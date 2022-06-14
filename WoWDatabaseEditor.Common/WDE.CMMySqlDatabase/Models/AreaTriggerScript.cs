using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "areatrigger_scripts")]
    public class AreaTriggerScriptWoTLK : IAreaTriggerScript
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public int Entry { get; set; }

        [Column(Name = "ScriptName")] 
        public string ScriptName { get; set; } = "";
    }
}