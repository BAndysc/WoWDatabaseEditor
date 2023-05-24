using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models
{
    [Table(Name = "npc_text")]
    public class NpcTextWoTLK : INpcText
    {
        [PrimaryKey]
        [Column(Name = "ID")]
        public uint Id { get; set; }

        public uint BroadcastTextId { get; set; }

        [Column(Name = "text0_0")]
        public string? Text0_0 { get; set; }
        
        [Column(Name = "text0_1")]
        public string? Text0_1 { get; set; }
    }
}