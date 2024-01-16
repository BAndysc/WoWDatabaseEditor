using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Parameters;

namespace WDE.Conditions.Data
{
    [ExcludeFromCodeCoverage]
    public struct ConditionParameterJsonData
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "values")]
        public Dictionary<long, SelectOption> Values { get; set; }
    }
    
    [ExcludeFromCodeCoverage]
    public struct ConditionStringParameterJsonData
    {
        [JsonProperty(PropertyName = "type")]
        public string? Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }


    [ExcludeFromCodeCoverage]
    public struct ConditionSourceTargetJsonData
    {
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "comment")]
        public string Comment { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public struct ConditionSourceParamsJsonData
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public struct ConditionSourcesJsonData
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "targets")]
        public Dictionary<int, ConditionSourceTargetJsonData> Targets { get; set; }

        [JsonProperty(PropertyName = "group")]
        public ConditionSourceParamsJsonData Group;

        [JsonProperty(PropertyName = "entry")]
        public ConditionSourceParamsJsonData Entry;

        [JsonProperty(PropertyName = "sourceId")]
        public ConditionSourceParamsJsonData SourceId;

        public IDatabaseProvider.ConditionKeyMask GetMask()
        {
            IDatabaseProvider.ConditionKeyMask mask = IDatabaseProvider.ConditionKeyMask.None;
            if (!string.IsNullOrEmpty(Group.Name))
                mask |= IDatabaseProvider.ConditionKeyMask.SourceGroup;
            if (!string.IsNullOrEmpty(Entry.Name))
                mask |= IDatabaseProvider.ConditionKeyMask.SourceEntry;
            if (!string.IsNullOrEmpty(SourceId.Name))
                mask |= IDatabaseProvider.ConditionKeyMask.SourceId;
            return mask;
        }
    }

    [ExcludeFromCodeCoverage]
    public class ConditionJsonData
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";

        [JsonProperty(PropertyName = "parameters")]
        public IList<ConditionParameterJsonData>? Parameters { get; set; }
        
        [JsonProperty(PropertyName = "sparameters")]
        public IList<ConditionStringParameterJsonData>? StringParameters { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; } = "Missing description";
        
        [JsonProperty(PropertyName = "negdescription")]
        public string? NegativeDescription { get; set; }
        
        [JsonProperty(PropertyName = "help")]
        public string? Help { get; set; }

        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; } = "Missing name";

        [JsonProperty(PropertyName = "tags")]
        public IList<string>? Tags { get; set; }
        
        public override string ToString()
        {
            return $"{NameReadable} {Id}";
        }
    }

    [ExcludeFromCodeCoverage]
    public struct ConditionGroupsJsonData
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "group_members")]
        public IList<string> Members { get; set; }
    }
}