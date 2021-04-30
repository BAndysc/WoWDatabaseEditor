using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "gossip_menu")]
    public class MySqlGossipMenuLine
    {
        [PrimaryKey]
        [Column(Name = "MenuID")]
        public uint MenuId { get; set; }

        [PrimaryKey]
        [Column(Name = "TextID")]
        public uint TextId { get; set; }
        
        public INpcText? Text { get; set; }

        internal MySqlGossipMenuLine SetText(INpcText text)
        {
            Text = text;
            return this;
        }
    }
}