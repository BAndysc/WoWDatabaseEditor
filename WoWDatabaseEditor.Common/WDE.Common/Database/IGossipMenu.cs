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
    
    public interface IGossipMenuOption
    {
        uint MenuId { get; }
        uint OptionIndex { get; }
        GossipOptionIcon Icon { get; }
        string? Text { get; }
        int BroadcastTextId { get; }
        GossipOption OptionType { get; }
        uint NpcFlag { get; }
        uint ActionMenuId { get; }
        uint ActionPoiId { get; }
        uint BoxCoded { get; }
        uint BoxMoney { get; }
        string? BoxText { get; }
        int BoxBroadcastTextId { get; }
        ushort VerifiedBuild { get; }
    }
}