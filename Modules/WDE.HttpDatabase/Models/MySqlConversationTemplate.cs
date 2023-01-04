
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonConversationTemplate : IConversationTemplate
    {
        
        public uint Id { get; set; }
        
        
        public uint FirstLineId { get; set; }

        
        public string ScriptName { get; set; } = "";
    }
}