﻿using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
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
    }

    [ExcludeFromCodeCoverage]
    public struct ConditionJsonData
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public IList<ConditionParameterJsonData> Parameters { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }
        
        [JsonProperty(PropertyName = "help")]
        public string Help { get; set; }
        
        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; }
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