using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.SkyFireMySqlDatabase.Models
{
    [Table(Name = "gossip_menu")]
    public class MySqlGossipMenuLine
    {
        [PrimaryKey]
        [Column(Name = "entry")]
        public uint MenuId { get; set; }

        [PrimaryKey]
        [Column(Name = "text_id")]
        public uint TextId { get; set; }
        
        public INpcText? Text { get; set; }

        internal MySqlGossipMenuLine SetText(INpcText text)
        {
            Text = text;
            return this;
        }
    }
}