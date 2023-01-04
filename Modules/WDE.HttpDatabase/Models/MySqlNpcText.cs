
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonNpcText : INpcText
    {
        
        public uint Id { get; set; }

        public uint BroadcastTextId { get; set; }

        
        public string? Text0_0 { get; set; }
        
        
        public string? Text0_1 { get; set; }
    }
}