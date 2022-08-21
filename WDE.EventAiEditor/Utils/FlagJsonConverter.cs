using System;
using Newtonsoft.Json;

namespace WDE.EventAiEditor.Utils
{
    public class FlagJsonConverter : JsonConverter
    {
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is not string value)
                return existingValue;

            var flags = value.Split(",");
            int val = 0;
            foreach (var flag in flags)
            {
                if (Enum.TryParse(objectType, flag.Trim(), out var f) && f != null)
                {
                    val |= (int) f;
                }
            }

            return val;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new Exception("Not yet implemented :(");
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}