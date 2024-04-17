using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;

public class JsonQuestRelation : IQuestRelation
{
    public uint Quest { get; set; }
    public uint Entry { get; set; }
    public QuestRelationObjectType Type { get; set; }
}