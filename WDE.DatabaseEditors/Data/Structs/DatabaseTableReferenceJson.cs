using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [ExcludeFromCodeCoverage]
    public class DatabaseTableReferenceJson
    {
        [JsonProperty(PropertyName = "file")] 
        public string File { get; set; } = "";
        
        [JsonProperty(PropertyName = "compatibility")] 
        public string Compatibility { get; set; } = "";
    }
}