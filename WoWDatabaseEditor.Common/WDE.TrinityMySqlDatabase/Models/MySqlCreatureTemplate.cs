using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models
{
    [Table(Name = "creature_template")]
    public class MySqlCreatureTemplate : ICreatureTemplate
    {
        [PrimaryKey]
        [Identity]
        [Column(Name = "entry")]
        public uint Entry { get; set; }

        [Column(Name = "modelid1")]
        public uint modelid1 { get; set; }

        // modelid 2 3 4

        [Column(Name = "scale")]
        public uint scale { get; set; }

        [Column(Name = "gossip_menu_id")] 
        public uint GossipMenuId { get; set; }

        [Column(Name = "name")] 
        public string Name { get; set; } = "";

        [Column(Name = "AIName")]
        public string AIName { get; set; } = "";

        [Column(Name = "ScriptName")]
        public string ScriptName { get; set; } = "";
    }
}