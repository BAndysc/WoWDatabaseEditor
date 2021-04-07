using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "spell_script_names")]
    public class MySqlSpellScriptName : ISpellScriptName
    {
        [Column(Name = "spell_id")]
        [PrimaryKey]
        public int SpellId { get; set; }
        
        [Column(Name = "ScriptName")]
        [PrimaryKey]
        public string ScriptName { get; set; } = "";
    }
}