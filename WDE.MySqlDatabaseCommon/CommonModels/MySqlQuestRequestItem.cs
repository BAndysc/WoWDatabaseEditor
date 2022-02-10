using LinqToDB.Mapping;
using WDE.Common.Database;

namespace WDE.MySqlDatabaseCommon.CommonModels;

[Table(Name = "quest_request_items")]
public class MySqlQuestRequestItem : IQuestRequestItem
{
    [PrimaryKey]
    [Column(Name = "ID")]
    public uint Entry { get; set; }
    
    [Column(Name = "EmoteOnComplete")]
    public uint EmoteOnComplete { get; set; }
    
    [Column(Name = "EmoteOnIncomplete")]
    public uint EmoteOnIncomplete { get; set; }
    
    [Column(Name = "CompletionText")]
    public string? CompletionText { get; set; }
    
    [Column(Name = "VerifiedBuild")]
    public int VerifiedBuild { get; set; }
}