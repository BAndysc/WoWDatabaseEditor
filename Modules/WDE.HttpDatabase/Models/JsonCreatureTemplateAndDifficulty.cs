using Newtonsoft.Json;

namespace WDE.HttpDatabase.Models;

public class JsonCreatureTemplateAndDifficulty
{
    [JsonProperty("templates")]
    public IReadOnlyList<JsonCreatureTemplateWrath> Templates { get; set; } = new List<JsonCreatureTemplateWrath>();

    [JsonProperty("difficulties")]
    public IReadOnlyList<JsonCreatureTemplateDifficulty> Difficulties { get; set; } = new List<JsonCreatureTemplateDifficulty>();
}