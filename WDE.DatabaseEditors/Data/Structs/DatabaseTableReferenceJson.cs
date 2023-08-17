using System.Diagnostics.CodeAnalysis;
using Generator.Equals;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [ExcludeFromCodeCoverage]
    [Equatable]
    public partial class DatabaseTableReferenceJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "file")] 
        public string File { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "compatibility")] 
        public string Compatibility { get; set; } = "";
    }
}