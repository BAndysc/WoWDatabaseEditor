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
        public int VerifiedBuild { get; set; }
        
        public bool HasOptionType => true;
    }
    
    [Table(Name = "gossip_menu_option")]
    public class MySqlGossipMenuOptionCata : IGossipMenuOption
    {
        [PrimaryKey]
        [Column(Name = "MenuId")]
        public uint MenuId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "OptionIndex")]
        public uint OptionIndex { get; set; }
        
        [Column(Name = "OptionIcon")]
        public GossipOptionIcon Icon { get; set; }
        
        [Column(Name = "OptionText")]
        public string? Text { get; set; }
        
        [Column(Name = "OptionBroadcastTextId")]
        public int BroadcastTextId { get; set; }
        
        [Column(Name = "OptionType")]
        public GossipOption OptionType { get; set; }
        
        [Column(Name = "OptionNpcFlag")]
        public uint NpcFlag { get; set; }

        public uint ActionMenuId => Action?.ActionMenuId ?? 0;
        public uint ActionPoiId => Action?.ActionPoiId ?? 0;
        public uint BoxCoded => Box?.BoxCoded ?? 0;
        public uint BoxMoney => Box?.BoxMoney ?? 0;
        public string? BoxText => Box?.BoxText;
        public int BoxBroadcastTextId => Box?.BoxBroadcastTextId ?? 0;
        public bool HasOptionType => true;

        [Column(Name = "VerifiedBuild")]
        public int VerifiedBuild { get; set; }

        public MySqlGossipMenuOptionAction? Action;
        public MySqlGossipMenuOptionBox? Box;

        public MySqlGossipMenuOptionCata SetAction(MySqlGossipMenuOptionAction action)
        {
            Action = action;
            return this;
        }
        
        public MySqlGossipMenuOptionCata SetBox(MySqlGossipMenuOptionBox box)
        {
            Box = box;
            return this;
        }
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
        public int VerifiedBuild { get; set; }
        
        public bool HasOptionType => false;
    }
    
    [Table(Name = "gossip_menu_option_action")]
    public class MySqlGossipMenuOptionAction
    {
        [PrimaryKey]
        [Column(Name = "MenuID")]
        public uint MenuId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "OptionIndex")]
        public uint OptionIndex { get; set; }
        
        [Column(Name = "ActionMenuId")]
        public uint ActionMenuId { get; set; }
        
        [Column(Name = "ActionPoiId")]
        public uint ActionPoiId { get; set; }
    }
    
    [Table(Name = "gossip_menu_option_box")]
    public class MySqlGossipMenuOptionBox
    {
        [PrimaryKey]
        [Column(Name = "MenuID")]
        public uint MenuId { get; set; }
        
        [PrimaryKey]
        [Column(Name = "OptionIndex")]
        public uint OptionIndex { get; set; }
     
        [Column(Name = "BoxCoded")]
        public uint BoxCoded { get; set; }
        
        [Column(Name = "BoxMoney")]
        public uint BoxMoney { get; set; }
        
        [Column(Name = "BoxText")]
        public string? BoxText { get; set; }
        
        [Column(Name = "BoxBroadcastTextID")]
        public int BoxBroadcastTextId { get; set; }   
    }
}