namespace WDE.Common.Database;

public enum QuestRelationObjectType
{
    Creature,
    GameObject
}

public interface IQuestRelation
{
    uint Quest { get; }
    uint Entry { get; }
    QuestRelationObjectType Type { get; }
}