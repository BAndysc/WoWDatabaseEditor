
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonCreatureText : ICreatureText
    {
        
        public uint CreatureId { get; set; }
        
        
        public byte GroupId { get; set; }
        
        
        public byte Id { get; set; }

        
        public string? Text { get; set; }

        
        public CreatureTextType Type { get; set; }

        
        public byte Language { get; set; }

        
        public float Probability { get; set; }

        
        public uint Emote { get; set; }

        
        public uint Duration { get; set; }

        
        public uint Sound { get; set; }

        
        public uint BroadcastTextId { get; set; }

        
        public CreatureTextRange TextRange { get; set; }

        
        public string? Comment { get; set; }
    }
}