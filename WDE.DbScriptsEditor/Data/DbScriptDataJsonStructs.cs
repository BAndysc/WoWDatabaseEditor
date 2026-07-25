using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using WDE.Common.Parameters;

namespace WDE.DbScriptsEditor.Data
{
    // Destination-based parameter definition. Each parameter names the physical column
    // (destination) it serializes to, rather than a positional param1..N slot.
    [ExcludeFromCodeCoverage]
    public struct DbScriptParameterJson
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "destination")]
        public string Destination { get; set; }

        [JsonProperty(PropertyName = "required")]
        public bool Required { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string? Description { get; set; }

        [JsonProperty(PropertyName = "defaultVal")]
        public long DefaultVal { get; set; }

        [JsonProperty(PropertyName = "values")]
        [JsonConverter(typeof(ParameterValuesJsonConverter))]
        public Dictionary<long, SelectOption>? Values { get; set; }
    }

    // Per-command description of the COMMAND_ADDITIONAL (data_flags 0x8) bit, rendered as an
    // inline two-option switch parameter so the user sees what the flag actually does.
    [ExcludeFromCodeCoverage]
    public struct DbScriptAdditionalFlagJson
    {
        [JsonProperty(PropertyName = "name")]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "off")]
        public string? Off { get; set; }

        [JsonProperty(PropertyName = "on")]
        public string? On { get; set; }
    }

    // A picker variant: appears as its own picker entry, but serializes to the same command id
    // with preset column values applied (used to flatten COMMAND_ADDITIONAL / selector params).
    [ExcludeFromCodeCoverage]
    public struct DbScriptVariantJson
    {
        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; }

        [JsonProperty(PropertyName = "help")]
        public string? Help { get; set; }

        [JsonProperty(PropertyName = "search_tags")]
        public string? SearchTags { get; set; }

        // column name -> preset. Value forms: "|0x008" (OR bits into the column),
        // or a plain integer literal (exact match / pre-fill).
        [JsonProperty(PropertyName = "preset")]
        public Dictionary<string, string>? Preset { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public IList<DbScriptParameterJson>? Parameters { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string? Description { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public struct DbScriptCommandJson
    {
        [JsonProperty(PropertyName = "id")]
        public uint Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "name_readable")]
        public string NameReadable { get; set; }

        [JsonProperty(PropertyName = "help")]
        public string? Help { get; set; }

        [JsonProperty(PropertyName = "search_tags")]
        public string? SearchTags { get; set; }

        [JsonProperty(PropertyName = "group")]
        public string? Group { get; set; }

        [JsonProperty(PropertyName = "deprecated")]
        public bool Deprecated { get; set; }

        // Structural capabilities (drive the source/target/buddy UI in phase 3).
        [JsonProperty(PropertyName = "source_types")]
        public IList<string>? SourceTypes { get; set; }

        [JsonProperty(PropertyName = "target_types")]
        public IList<string>? TargetTypes { get; set; }

        [JsonProperty(PropertyName = "buddy")]
        public string? Buddy { get; set; }

        [JsonProperty(PropertyName = "supports_additional_flag")]
        public bool SupportsAdditionalFlag { get; set; }

        [JsonProperty(PropertyName = "additional_flag")]
        public DbScriptAdditionalFlagJson? AdditionalFlag { get; set; }

        [JsonProperty(PropertyName = "parameters")]
        public IList<DbScriptParameterJson>? Parameters { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "variants")]
        public IList<DbScriptVariantJson>? Variants { get; set; }

        public bool HasParameters() => Parameters != null && Parameters.Count > 0;
    }

    [ExcludeFromCodeCoverage]
    public struct DbScriptGroupJson
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "group_members")]
        public IList<string>? Members { get; set; }
    }
}
