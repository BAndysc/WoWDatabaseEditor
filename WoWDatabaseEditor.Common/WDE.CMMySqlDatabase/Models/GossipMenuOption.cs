using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "gossip_menu_option")]
public class GossipMenuOption : IGossipMenuOption
{
    [PrimaryKey]
    [Column(Name = "menu_id")]
    public uint MenuId { get; set; }
        
    [PrimaryKey]
    [Column(Name = "id")]
    public uint OptionIndex { get; set; }
        
    [Column(Name = "option_icon")]
    public GossipOptionIcon Icon { get; set; }
        
    [Column(Name = "option_text")]
    public string? Text { get; set; }
        
    [Column(Name = "option_broadcast_text")]
    public int BroadcastTextId { get; set; }
        
    [Column(Name = "option_id")]
    public GossipOption OptionType { get; set; }
        
    [Column(Name = "npc_option_npcflag")]
    public uint NpcFlag { get; set; }
        
    [Column(Name = "action_menu_id")]
    public int ActionMenuId { get; set; }
        
    [Column(Name = "action_poi_id")]
    public uint ActionPoiId { get; set; }

    public uint ActionScriptId => 0;

    [Column(Name = "box_coded")]
    public uint BoxCoded { get; set; }
        
    [Column(Name = "box_money")]
    public uint BoxMoney { get; set; }
        
    [Column(Name = "box_text")]
    public string? BoxText { get; set; }
        
    [Column(Name = "box_broadcast_text")]
    public int BoxBroadcastTextId { get; set; }

    public int VerifiedBuild => 0;
    public bool HasOptionType => true;
}