using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "gossip_menu_option")]
    public class MySqlGossipMenuOption : IGossipMenuOption
    {
        [PrimaryKey]
        [Column(Name = "MenuID")]
        public uint MenuId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "OptionID")]
        public uint OptionIndex { get; set; }
        
        [Column(Name = "OptionIcon")]
        public GossipOptionIcon Icon { get; set; }
        
        [Column(Name = "OptionText")]
        public string? Text { get; set; }
        
        [Column(Name = "OptionBroadcastTextID")]
        public int BroadcastTextId { get; set; }
        
        [Column(Name = "OptionType")]
        public GossipOption OptionType { get; set; }
        
        [Column(Name = "OptionNpcFlag")]
        public uint NpcFlag { get; set; }
        
        [Column(Name = "ActionMenuID")]
        public uint ActionMenuId { get; set; }
        
        [Column(Name = "ActionPoiID")]
        public uint ActionPoiId { get; set; }
        
        [Column(Name = "BoxCoded")]
        public uint BoxCoded { get; set; }
        
        [Column(Name = "BoxMoney")]
        public uint BoxMoney { get; set; }
        
        [Column(Name = "BoxText")]
        public string? BoxText { get; set; }
        
        [Column(Name = "BoxBroadcastTextID")]
        public int BoxBroadcastTextId { get; set; }
        
        [Column(Name = "VerifiedBuild")]
        public ushort VerifiedBuild { get; set; }
    }
}