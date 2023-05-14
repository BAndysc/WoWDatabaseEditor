using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "spell_scripts")]
    public class SpellScriptNameWoTLK : ISpellScriptName
    {
        [Column(Name = "Id")]
        [PrimaryKey]
        public int SpellId { get; set; }
        
        [Column(Name = "ScriptName")]
        [PrimaryKey]
        public string ScriptName { get; set; } = "";
    }
}