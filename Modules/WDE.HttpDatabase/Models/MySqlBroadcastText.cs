
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonBroadcastText : IBroadcastText
    {
        
        public uint Id { get; set;}
        
        
        public uint Language { get; set;}

        
        public string? Text { get; set;}
        
        
        public string? Text1 { get; set; }
        
        
        public uint EmoteId1  { get; set; }
        
        
        public uint EmoteId2 { get; set; }
        
        
        public uint EmoteId3 { get; set; }
        
        
        public uint EmoteDelay1 { get; set; }
        
        
        public uint EmoteDelay2 { get; set; }
        
        
        public uint EmoteDelay3 { get; set; }

        public uint Sound1 { get; set; }
        
        public uint Sound2 { get; set; }

        
        public uint SoundEntriesId { get; set; }
        
        
        public uint EmotesId { get; set; }
        
        
        public uint Flags { get; set; }
    }
    

    public class JsonBroadcastTextAzeroth : IBroadcastText
    {
        
        public uint Id { get; set;}
        
        
        public uint Language { get; set;}

        
        public string? Text { get; set;}
        
        
        public string? Text1 { get; set; }
        
        
        public uint EmoteId1  { get; set; }
        
        
        public uint EmoteId2 { get; set; }
        
        
        public uint EmoteId3 { get; set; }
        
        
        public uint EmoteDelay1 { get; set; }
        
        
        public uint EmoteDelay2 { get; set; }
        
        
        public uint EmoteDelay3 { get; set; }

        public uint Sound1 { get; set; }

        public uint Sound2 { get; set; }

        
        public uint SoundEntriesId { get; set; }
        
        
        public uint EmotesId { get; set; }
        
        
        public uint Flags { get; set; }
    }
}