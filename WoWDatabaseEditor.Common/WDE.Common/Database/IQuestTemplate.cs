using System;

namespace WDE.Common.Database
{
    public interface IQuestTemplate
    {
        uint Entry { get; }
        string Name { get; }
        int MinLevel { get; }
        int QuestSortId { get; }
        QuestFlags Flags { get; }
        
        CharacterClasses AllowableClasses { get; }
        CharacterRaces AllowableRaces { get; }
        
        int PrevQuestId { get; }
        int NextQuestId { get; }
        int ExclusiveGroup { get; }
        int BreadcrumbForQuestId { get; }
        uint NextQuestInChain { get; }
        
        uint RewardMailTemplateId { get; }
        uint QuestRewardId { get; }
    }

    public class AbstractQuestTemplate : IQuestTemplate
    {
        public uint Entry { get; set; }
        public string Name { get; set; } = "";
        public int MinLevel { get; set; }
        public int QuestSortId { get; set; }
        public QuestFlags Flags { get; set; }
        public CharacterClasses AllowableClasses { get; set; }
        public CharacterRaces AllowableRaces { get; set; }
        public int PrevQuestId { get; set; }
        public int NextQuestId { get; set; }
        public int ExclusiveGroup { get; set; }
        public int BreadcrumbForQuestId { get; set; }
        public uint NextQuestInChain { get; set; }
        public uint RewardMailTemplateId { get; set; }
        public uint QuestRewardId { get; set; }
    }

    public interface IQuestObjective
    {
        uint ObjectiveId { get; }
        uint QuestId { get; }
        QuestObjectiveType Type { get; }
        int StorageIndex { get; }
        int OrderIndex { get; }
        int ObjectId { get; }
        int Amount { get; }
        string? Description { get; }
    }

    public class AbstractQuestObjective : IQuestObjective
    {
        public uint ObjectiveId { get; set; }
        public uint QuestId { get; set; }
        public QuestObjectiveType Type { get; set; }
        public int StorageIndex { get; set; }
        public int OrderIndex { get; set; }
        public int ObjectId { get; set; }
        public int Amount { get; set; }
        public string? Description { get; set; }
    }
    
    public enum QuestObjectiveType : ushort
    {
        Monster = 0,
        Item = 1,
        GameObject = 2,
        TalkTo = 3,
        Currency = 4,
        LearnSpell = 5,
        MinReputation = 6,
        MaxReputation = 7,
        Money = 8,
        PlayerKills = 9,
        AreaTrigger = 10,
        WinPetBattleAgainstNpc = 11,
        DefeatBattlePet = 12,
        WinPvPPetBattles = 13,
        CriteriaTree = 14,
        ProgressBar = 15,
        HaveCurrency = 16,
        ObtainCurrency = 17,
    }

    [Flags]
    public enum QuestFlags : uint
    {
        None                        = 0x00000000,
        CompletionNoDeath         = 0x00000001,
        CompletionEvent            = 0x00000002,
        CompletionAreaTrigger     = 0x00000004,
        Sharable                    = 0x00000008,   // Can be shared: Player::CanShareQuest()
        HasCondition               = 0x00000010,   // Not used currently
        HideRewardPoi             = 0x00000020,   // Hides questgiver turn-in minimap icon
        RaidGroupOk               = 0x00000040,   // Can be completed while in raid
        WarModeRewardsOptIn     = 0x00000080,   // Not used currently
        NoMoneyForXp             = 0x00000100,   // Experience is not converted to gold at max level
        HideReward                 = 0x00000200,   // Items and money rewarded only sent in SMSG_QUESTGIVER_OFFER_REWARD (not in SMSG_QUEST_GIVER_QUEST_DETAILS or in client quest log(SMSG_QUEST_QUERY_RESPONSE))
        TrackingEvent              = 0x00000400,   // These quests are automatically rewarded on quest complete and they will never appear in quest log client side.
        DeprecateReputation        = 0x00000800,   // Not used currently
        Daily                       = 0x00001000,   // Used to know quest is Daily one
        FlagsPvp                   = 0x00002000,   // Having this quest in log forces PvP flag
        Deprecated                  = 0x00004000,   // Used on quests that are not generally available
        Weekly                      = 0x00008000,
        AutoComplete               = 0x00010000,   // Quests with this flag player submit automatically by special button in player gui
        DisplayItemInTracker     = 0x00020000,   // Displays usable item in quest tracker
        DisableCompletionText     = 0x00040000,   // use Objective text as Complete text
        AutoAccept                 = 0x00080000,   // The client recognizes this flag as auto-accept.
        PlayerCastAccept          = 0x00100000,   // Accept Spell Player Cast
        PlayerCastComplete        = 0x00200000,   // Complete Spell Player Cast
        UpdatePhaseshift           = 0x00400000,   // Update Phase Shift
        SorWhitelist               = 0x00800000,   // Scroll of Resurrection Whitelist
        LaunchGossipComplete      = 0x01000000,   // Gossip on Quest Completion - Force Gossip
        RemoveSurplusItems        = 0x02000000,   // Remove all items from inventory that have the same id as the objective, not just the amount required by quest
        WellKnown                  = 0x04000000,   // Well-Known
        PortraitInQuestLog       = 0x08000000,   // Portrait from Log
        ShowItemWhenCompleted    = 0x10000000,   // Show Item When Completed
        LaunchGossipAccept        = 0x20000000,   // Gossip on Quest Accept - Force Gossip
        ItemsGlowWhenComplete    = 0x40000000,   // Items Glow When Done
        FailOnLogout              = 0x80000000    // Fail on Logout
    }
}