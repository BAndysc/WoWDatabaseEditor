using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels
{
    [Table(Name = "gossip_menu_option")]
    public class MySqlGossipMenuOptionWrath : IGossipMenuOption
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
        public int ActionMenuId { get; set; }
        
        [Column(Name = "ActionPoiID")]
        public uint ActionPoiId { get; set; }

        public uint ActionScriptId => 0;

        [Column(Name = "BoxCoded")]
        public uint BoxCoded { get; set; }
        
        [Column(Name = "BoxMoney")]
        public uint BoxMoney { get; set; }
        
        [Column(Name = "BoxText")]
        public string? BoxText { get; set; }
        
        [Column(Name = "BoxBroadcastTextID")]
        public int BoxBroadcastTextId { get; set; }
        
        [Column(Name = "VerifiedBuild")]
        public int VerifiedBuild { get; set; }
        
        public bool HasOptionType => true;
    }
    
    [Table(Name = "gossip_menu_option")]
    public class MySqlGossipMenuOptionMaster : IGossipMenuOption
    {
        [PrimaryKey]
        [Column(Name = "MenuID")]
        public uint MenuId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "OptionID")]
        public uint OptionIndex { get; set; }
        
        [Column(Name = "OptionNpc")]
        public GossipOptionIcon Icon { get; set; }
        
        [Column(Name = "OptionText")]
        public string? Text { get; set; }
        
        [Column(Name = "OptionBroadcastTextID")]
        public int BroadcastTextId { get; set; }

        public GossipOption OptionType => GossipOption.None;

        public uint NpcFlag { get; set; }
        
        [Column(Name = "ActionMenuID")]
        public int ActionMenuId { get; set; }
        
        [Column(Name = "ActionPoiID")]
        public uint ActionPoiId { get; set; }

        public uint ActionScriptId => 0;

        [Column(Name = "BoxCoded")]
        public uint BoxCoded { get; set; }
        
        [Column(Name = "BoxMoney")]
        public uint BoxMoney { get; set; }
        
        [Column(Name = "BoxText")]
        public string? BoxText { get; set; }
        
        [Column(Name = "BoxBroadcastTextID")]
        public int BoxBroadcastTextId { get; set; }
        
        [Column(Name = "VerifiedBuild")]
        public int VerifiedBuild { get; set; }
        
        public bool HasOptionType => false;
    }
}