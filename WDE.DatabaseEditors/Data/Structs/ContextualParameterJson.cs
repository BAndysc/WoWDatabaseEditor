using System.Collections.Generic;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs;

public class ContextualParameterJson
{
    [JsonProperty("name")]
    public string Name { get; set; } = "";
    
    [JsonProperty("simple_switch")]
    public ContextualParameterSimpleSwitchJson? SimpleSwitch { get; set; }
    
    [JsonProperty("simple_string_switch")]
    public ContextualParameterSimpleStringSwitchJson? SimpleStringSwitch { get; set; }
}


public class ContextualParameterSimpleSwitchJson
{
    [JsonProperty("column")]
    public string Column { get; set; } = "";

    [JsonProperty("default")]
    public string? Default { get; set; }

    [JsonProperty("values")]
    public Dictionary<long, string> Values { get; set; } = null!;
}

public class ContextualParameterSimpleStringSwitchJson
{
    [JsonProperty("column")]
    public string Column { get; set; } = "";

    [JsonProperty("default")]
    public string? Default { get; set; }

    [JsonProperty("values")]
    public Dictionary<string, string> Values { get; set; } = null!;
}