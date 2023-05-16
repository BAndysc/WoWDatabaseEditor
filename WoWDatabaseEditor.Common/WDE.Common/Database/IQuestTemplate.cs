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
    }

    public interface IQuestObjective
    {
        uint ObjectiveId { get; }
        uint QuestId { get; }
        QuestObjectiveType Type { get; }
        int StorageIndex { get; }
        int ObjectId { get; }
        int Amount { get; }
        string? Description { get; }
    }
    
    public enum QuestObjectiveType : ushort
    {
        Monster = 0,
        Item = 1,
        GamObject = 2,
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