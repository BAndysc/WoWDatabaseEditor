using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "spell_script_names")]
    public class SpellScriptNameWoTLK : ISpellScriptName
    {
        [Column(Name = "spell_id")]
        [PrimaryKey]
        public int SpellId { get; set; }
        
        [Column(Name = "ScriptName")]
        [PrimaryKey]
        public string ScriptName { get; set; } = "";
    }
}