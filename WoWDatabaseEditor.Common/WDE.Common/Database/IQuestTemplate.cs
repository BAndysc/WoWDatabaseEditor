namespace WDE.Common.Database
{
    public interface IQuestTemplate
    {
        uint Entry { get; }
        string Name { get; }
        int MinLevel { get; }
        int QuestSortId { get; }
        
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
}