using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "broadcast_text")]
    public class MySqlBroadcastText : IBroadcastText
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Id { get; set;}
        
        [Column(Name = "LanguageID")]
        public uint Language { get; set;}

        [Column(Name = "Text")]
        public string? Text { get; set;}
        
        [Column(Name = "Text1")]
        public string? Text1 { get; set; }
        
        [Column(Name = "EmoteID1")]
        public uint EmoteId1  { get; set; }
        
        [Column(Name = "EmoteID2")]
        public uint EmoteId2 { get; set; }
        
        [Column(Name = "EmoteID3")]
        public uint EmoteId3 { get; set; }
        
        [Column(Name = "EmoteDelay1")]
        public uint EmoteDelay1 { get; set; }
        
        [Column(Name = "EmoteDelay2")]
        public uint EmoteDelay2 { get; set; }
        
        [Column(Name = "EmoteDelay3")]
        public uint EmoteDelay3 { get; set; }

        public uint Sound1 { get; set; }
        
        public uint Sound2 { get; set; }

        [Column(Name = "SoundEntriesID")]
        public uint SoundEntriesId { get; set; }
        
        [Column(Name = "EmotesID")]
        public uint EmotesId { get; set; }
        
        [Column(Name = "Flags")]
        public uint Flags { get; set; }
    }
    
    [Table(Name = "broadcast_text")]
    public class MySqlBroadcastTextAzeroth : IBroadcastText
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Id { get; set;}
        
        [Column(Name = "LanguageId")]
        public uint Language { get; set;}

        [Column(Name = "MaleText")]
        public string? Text { get; set;}
        
        [Column(Name = "FemaleText")]
        public string? Text1 { get; set; }
        
        [Column(Name = "EmoteID1")]
        public uint EmoteId1  { get; set; }
        
        [Column(Name = "EmoteID2")]
        public uint EmoteId2 { get; set; }
        
        [Column(Name = "EmoteID3")]
        public uint EmoteId3 { get; set; }
        
        [Column(Name = "EmoteDelay1")]
        public uint EmoteDelay1 { get; set; }
        
        [Column(Name = "EmoteDelay2")]
        public uint EmoteDelay2 { get; set; }
        
        [Column(Name = "EmoteDelay3")]
        public uint EmoteDelay3 { get; set; }

        public uint Sound1 { get; set; }

        public uint Sound2 { get; set; }

        [Column(Name = "SoundEntriesId")]
        public uint SoundEntriesId { get; set; }
        
        [Column(Name = "EmotesID")]
        public uint EmotesId { get; set; }
        
        [Column(Name = "Flags")]
        public uint Flags { get; set; }
    }
}