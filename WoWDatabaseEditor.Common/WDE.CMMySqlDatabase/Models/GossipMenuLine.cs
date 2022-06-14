using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "gossip_menu")]
    public class GossipMenuLineWoTLK
    {
        [Column("entry"       , IsPrimaryKey = true, PrimaryKeyOrder = 0)] public ushort MenuId      { get; set; } // smallint(6) unsigned
        [Column("text_id"     , IsPrimaryKey = true, PrimaryKeyOrder = 1)] public uint   TextId      { get; set; } // mediumint(8) unsigned
        
        public INpcText? Text { get; set; }

        internal GossipMenuLineWoTLK SetText(INpcText text)
        {
            Text = text;
            return this;
        }
    }
}