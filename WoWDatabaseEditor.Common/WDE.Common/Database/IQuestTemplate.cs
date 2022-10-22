namespace WDE.Common.Database
{
    public interface IQuestTemplate
    {
        uint Entry { get; }
        string Name { get; }

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
        int StorageIndex { get; }
        int ObjectId { get; }
        int Amount { get; }
        string? Description { get; }
    }
}