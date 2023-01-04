
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonGameEvent : IGameEvent
    {
        
        public ushort Entry { get; set; }

        
        public string? Description { get; set; }
    }
}