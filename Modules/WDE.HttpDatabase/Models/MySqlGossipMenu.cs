using Newtonsoft.Json;
using WDE.Common.Database;

namespace WDE.HttpDatabase.Models
{
    public class JsonGossipMenu : IGossipMenu
    {
        [JsonConstructor]
        public JsonGossipMenu(uint menuId, List<JsonNpcText>? texts)
        {
            MenuId = menuId;
            Texts = texts?.ToList() ?? new List<JsonNpcText>();
        }

        public uint MenuId { get; set; }

        [JsonProperty("Text")]
        public List<JsonNpcText> Texts { get; set; }

        [JsonIgnore]
        public IEnumerable<INpcText> Text => Texts;
    }
}