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