using Newtonsoft.Json;

namespace WDE.EventScriptsEditor.EventScriptData;

public class EventScriptRawData
{
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("description")] 
    public string Description { get; set; } = "";
    
    [JsonProperty("datalong")] 
    public string DataLong { get; set; } = "Parameter";
    
    [JsonProperty("datalong2")] 
    public string DataLong2 { get; set; } = "Parameter";
    
    [JsonProperty("dataint")] 
    public string DataInt { get; set; } = "Parameter";
}