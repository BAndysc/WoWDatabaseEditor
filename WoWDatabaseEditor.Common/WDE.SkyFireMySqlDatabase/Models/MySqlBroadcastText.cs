using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
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
    }
}