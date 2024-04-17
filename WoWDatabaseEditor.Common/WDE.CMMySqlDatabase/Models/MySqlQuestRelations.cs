using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.CMMySqlDatabase.Models;

[Table(Name = "creature_questrelation")]
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

[Table(Name = "gameobject_questrelation")]
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

[Table(Name = "creature_involvedrelation")]
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

[Table(Name = "gameobject_involvedrelation")]
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