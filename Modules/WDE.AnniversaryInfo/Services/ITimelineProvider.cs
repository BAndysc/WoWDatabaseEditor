using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WDE.AnniversaryInfo.Services;

public interface ITimelineProvider
{
    Task<IList<TimeLineItem>> GetTimeline();
    void OpenWeb();
}

public class TimeLineItem
{
    [JsonPropertyName("skip")] 
    public bool Skip { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; } = "";
    
    [JsonPropertyName("bg")]
    public string? Background { get; set; }

    [JsonPropertyName("content")] 
    public List<string> Content { get; set; } = new();
}