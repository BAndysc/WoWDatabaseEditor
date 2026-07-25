using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WDE.CMangosConditions.Data
{
    /// <summary>
    /// What kind of object a condition naturally operates on. Drives the {target}/{source}
    /// wording in the readable text when no caller context names the actual actors. Loaded from
    /// the "tags" array in conditions.json (e.g. ["player"], ["worldobject"], ["source-creature"]).
    /// </summary>
    [Flags]
    [JsonConverter(typeof(MangosConditionSubjectConverter))]
    public enum MangosConditionSubject
    {
        None = 0,
        Player = 1 << 0,
        Unit = 1 << 1,
        WorldObject = 1 << 2,
        Map = 1 << 3,
        SourceCreature = 1 << 4,
    }

    public static class MangosConditionSubjectExtensions
    {
        /// <summary>The word used for {target} when no caller context names the actor.</summary>
        public static string DefaultTargetName(this MangosConditionSubject subject) =>
            subject.HasFlag(MangosConditionSubject.Player) ? "player"
            : subject.HasFlag(MangosConditionSubject.WorldObject) ? "object"
            : "target";
    }

    /// <summary>Reads the conditions.json "tags" string array (or a single string) into the
    /// flags enum, tolerant of case and dashes/underscores; unknown names are ignored.</summary>
    internal class MangosConditionSubjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(MangosConditionSubject);

        public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            var result = MangosConditionSubject.None;
            if (reader.TokenType == JsonToken.StartArray)
            {
                foreach (var token in JArray.Load(reader))
                    result |= Parse((string?)token);
            }
            else if (reader.TokenType == JsonToken.String)
            {
                result = Parse((string?)reader.Value);
            }
            return result;
        }

        private static MangosConditionSubject Parse(string? value)
        {
            if (string.IsNullOrEmpty(value))
                return MangosConditionSubject.None;
            var normalized = value.Replace("-", "").Replace("_", "");
            return Enum.TryParse<MangosConditionSubject>(normalized, ignoreCase: true, out var parsed)
                ? parsed
                : MangosConditionSubject.None;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            var subject = value is MangosConditionSubject s ? s : MangosConditionSubject.None;
            writer.WriteStartArray();
            foreach (MangosConditionSubject flag in Enum.GetValues(typeof(MangosConditionSubject)))
                if (flag != MangosConditionSubject.None && subject.HasFlag(flag))
                    writer.WriteValue(flag.ToString().ToLowerInvariant());
            writer.WriteEndArray();
        }
    }
}
