using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "quest_objectives")]
public class MySqlQuestObjective : IQuestObjective
{
    [PrimaryKey]
    [Column(Name = "ID")]
    public uint ObjectiveId { get; set; }
    
    [Column(Name = "QuestID")]
    public uint QuestId { get; set; }

    [Column(Name = "Type")]
    public QuestObjectiveType Type { get; set; }

    [Column(Name = "StorageIndex")]
    public int StorageIndex { get; set; }

    [Column(Name = "Order")]
    public int OrderIndex { get; set; }

    [Column(Name = "ObjectID")]
    public int ObjectId { get; set; }
    
    [Column(Name = "Amount")]
    public int Amount { get; set; }
    
    [Column(Name = "Description")]
    public string? Description { get; set; }
}
