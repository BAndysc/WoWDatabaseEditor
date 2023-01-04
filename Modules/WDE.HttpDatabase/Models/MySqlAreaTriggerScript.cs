
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonAreaTriggerScript : IAreaTriggerScript
    {
        
        public int Entry { get; set; }

         
        public string ScriptName { get; set; } = "";
    }
}