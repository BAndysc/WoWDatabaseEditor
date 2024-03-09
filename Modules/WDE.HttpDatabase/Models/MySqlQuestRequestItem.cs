
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models;


public class JsonQuestRequestItem : IQuestRequestItem
{
    
    
    public uint Entry { get; set; }
    
    
    public uint EmoteOnComplete { get; set; }
    
    
    public uint EmoteOnIncomplete { get; set; }
    
    
    public string? CompletionText { get; set; }
    
    
    public int VerifiedBuild { get; set; }
}