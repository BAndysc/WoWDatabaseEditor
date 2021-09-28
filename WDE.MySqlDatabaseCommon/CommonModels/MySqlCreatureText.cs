using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "creature_text")]
    public class MySqlCreatureText : ICreatureText
    {
        [PrimaryKey]
        [Column(Name = "CreatureID")]
        public uint CreatureId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "GroupID")]
        public byte GroupId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "ID")]
        public byte Id { get; set; }

        [Column(Name = "Text")]
        public string? Text { get; set; }

        [Column(Name = "Type")]
        public CreatureTextType Type { get; set; }

        [Column(Name = "Language")]
        public byte Language { get; set; }

        [Column(Name = "Probability")]
        public float Probability { get; set; }

        [Column(Name = "Emote")]
        public uint Emote { get; set; }

        [Column(Name = "Duration")]
        public uint Duration { get; set; }

        [Column(Name = "Sound")]
        public uint Sound { get; set; }

        [Column(Name = "BroadcastTextId")]
        public uint BroadcastTextId { get; set; }

        [Column(Name = "TextRange")]
        public CreatureTextRange TextRange { get; set; }

        [Column(Name = "comment")]
        public string? Comment { get; set; }
    }
}