using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.TrinityMySqlDatabase.Models;

[Table(Name = "creature_queststarter")]
public class CreatureQuestStarter : IQuestRelation
{
    [PrimaryKey]
    [Column(Name = "quest")]
    public uint Quest { get; set; }

    [PrimaryKey]
    [Column(Name = "id")]
    public uint Entry { get; set; }

    public QuestRelationObjectType Type => QuestRelationObjectType.Creature;
}

[Table(Name = "gameobject_queststarter")]
public class GameObjectQuestStarter : IQuestRelation
{
    [PrimaryKey]
    [Column(Name = "quest")]
    public uint Quest { get; set; }

    [PrimaryKey]
    [Column(Name = "id")]
    public uint Entry { get; set; }

    public QuestRelationObjectType Type => QuestRelationObjectType.GameObject;
}

[Table(Name = "creature_questender")]
public class CreatureQuestEnder : IQuestRelation
{
    [PrimaryKey]
    [Column(Name = "quest")]
    public uint Quest { get; set; }

    [PrimaryKey]
    [Column(Name = "id")]
    public uint Entry { get; set; }

    public QuestRelationObjectType Type => QuestRelationObjectType.Creature;
}

[Table(Name = "gameobject_questender")]
public class GameObjectQuestEnder : IQuestRelation
{
    [PrimaryKey]
    [Column(Name = "quest")]
    public uint Quest { get; set; }

    [PrimaryKey]
    [Column(Name = "id")]
    public uint Entry { get; set; }

    public QuestRelationObjectType Type => QuestRelationObjectType.GameObject;
}