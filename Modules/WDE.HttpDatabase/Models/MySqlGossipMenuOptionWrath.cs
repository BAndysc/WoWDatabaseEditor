
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{

    public class JsonGossipMenuOptionWrath : IGossipMenuOption
    {
        
        public uint MenuId { get; set; }
        
        
        public uint OptionIndex { get; set; }
        
        
        public GossipOptionIcon Icon { get; set; }
        
        
        public string? Text { get; set; }
        
        
        public int BroadcastTextId { get; set; }
        
        
        public GossipOption OptionType { get; set; }
        
        
        public uint NpcFlag { get; set; }
        
        
        public int ActionMenuId { get; set; }
        
        
        public uint ActionPoiId { get; set; }

        public uint ActionScriptId => 0;

        
        public uint BoxCoded { get; set; }
        
        
        public uint BoxMoney { get; set; }
        
        
        public string? BoxText { get; set; }
        
        
        public int BoxBroadcastTextId { get; set; }
        
        
        public int VerifiedBuild { get; set; }
        
        public bool HasOptionType => true;
    }
    

    public class JsonGossipMenuOptionMaster : IGossipMenuOption
    {
        
        public uint MenuId { get; set; }
        
        
        public uint OptionIndex { get; set; }
        
        
        public GossipOptionIcon Icon { get; set; }
        
        
        public string? Text { get; set; }
        
        
        public int BroadcastTextId { get; set; }

        public GossipOption OptionType => GossipOption.None;

        public uint NpcFlag { get; set; }
        
        
        public int ActionMenuId { get; set; }
        
        
        public uint ActionPoiId { get; set; }

        public uint ActionScriptId => 0;

        
        public uint BoxCoded { get; set; }
        
        
        public uint BoxMoney { get; set; }
        
        
        public string? BoxText { get; set; }
        
        
        public int BoxBroadcastTextId { get; set; }
        
        
        public int VerifiedBuild { get; set; }
        
        public bool HasOptionType => false;
    }
}