
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonSpellScriptName : ISpellScriptName
    {
        
        public int SpellId { get; set; }
        
        
        public string ScriptName { get; set; } = "";
    }
}