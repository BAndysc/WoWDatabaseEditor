
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonQuestObjective : IQuestObjective
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
