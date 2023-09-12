using System.Collections.Generic;

namespace WDE.Common.Database
{
    public enum GossipOptionIcon
    {
        Chat                = 0,
        Vendor              = 1,
        Taxi                = 2,
        Trainer             = 3,
        Interact1          = 4,
        Interact2          = 5,
        MoneyBag           = 6,
        Talk                = 7,
        Tabard              = 8,
        Battle              = 9,
        Dot                 = 10,
        Chat11             = 11,
        Chat12             = 12,
        Chat13             = 13,
        Unk14              = 14,
        Unk15              = 15,
        Chat16             = 16,
        Chat17             = 17,
        Chat18             = 18,
        Chat19             = 19,
        Chat20             = 20,
    }
        
    public enum GossipOption
    {
        None              = 0,
        Gossip            = 1,
        QuestGiver        = 2,
        Vendor            = 3,
        TaxiVendor        = 4,
        Trainer           = 5,
        SpiritHealer      = 6,
        SpiritGuide       = 7,
        Innkeeper         = 8,
        Banker            = 9,
        Petitioner        = 10,
        TabardDesigner    = 11,
        Battlefield       = 12,
        Auctioneer        = 13,
        StablePet         = 14,
        Armorer           = 15,
        UnlearnTalents    = 16,
        UnlearnPetTalents = 17,
        LearnDualSpec     = 18,
        OutdoorPvP        = 19,
        DualSpecInfo     = 20,
    }
    
    public interface IGossipMenu
    {
        uint MenuId { get; }
        IEnumerable<INpcText> Text { get; }
    }

    public interface IGossipMenuLine
    {
        uint MenuId { get; }
        uint TextId { get; }

        /// <summary>
        /// a special field used only in IQueryGenerator&lt;IGossipMenuOption&gt;.
        /// this is the comment outputted in the sql query.
        /// We might need a better way  for handling this type of comments
        /// </summary>
        // ReSharper disable once InconsistentNaming
        string? __comment { get; }
    }
    
    public interface IGossipMenuOption
    {
        uint MenuId { get; }
        uint OptionIndex { get; }
        GossipOptionIcon Icon { get; }
        string? Text { get; }
        int BroadcastTextId { get; }
        GossipOption OptionType { get; }
        uint NpcFlag { get; }
        int ActionMenuId { get; }
        uint ActionPoiId { get; }
        uint ActionScriptId { get; }
        uint BoxCoded { get; }
        uint BoxMoney { get; }
        string? BoxText { get; }
        int BoxBroadcastTextId { get; }
        int VerifiedBuild { get; }
        bool HasOptionType { get; }

        /// <summary>
        /// a special field used only in IQueryGenerator&lt;IGossipMenuOption&gt;.
        /// this is the comment outputted in the sql query.
        /// We might need a better way  for handling this type of comments
        /// </summary>
        // ReSharper disable once InconsistentNaming
        string? __comment => null;
        

        /// <summary>
        /// a special field used only in IQueryGenerator&lt;IGossipMenuOption&gt;.
        /// If true, the record will not be inserted to the database.
        /// We might need a better way  for handling this type of comments
        /// </summary>
        // ReSharper disable once InconsistentNaming
        bool __ignored => false;
    }

    public class AbstractGossipMenuOption : IGossipMenuOption
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
        public uint ActionScriptId { get; set; }
        public uint BoxCoded { get; set; }
        public uint BoxMoney { get; set; }
        public string? BoxText { get; set; }
        public int BoxBroadcastTextId { get; set; }
        public int VerifiedBuild { get; set; }
        public bool HasOptionType { get; set; }

        public string? __comment { get; set; }
        public bool __ignored { get; set; }
    }

    public class AbstractGossipMenuLine : IGossipMenuLine
    {
        public uint MenuId { get; set; }
        public uint TextId { get; set; }
        public string? __comment { get; set; }
    }
}