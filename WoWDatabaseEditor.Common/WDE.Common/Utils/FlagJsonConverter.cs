using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WDE.Common.Utils
{
    public class FlagJsonConverter : JsonConverter
    {
        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is string value)
            {
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
            else if (reader.TokenType == JsonToken.StartArray)
            {
                int val = 0;
                reader.Read();
                do
                {
                    if (reader.TokenType == JsonToken.EndArray)
                        break;
                    if (reader.TokenType == JsonToken.String)
                    {
                        var tokenValue = ((string)reader.Value!).Trim();
                        if (Enum.TryParse(objectType, tokenValue, out var f) && f != null)
                        {
                            val |= (int) f;
                        }
                        else
                            throw new Exception("Invalid flag value: " + tokenValue);
                    }
                    else
                        throw new Exception("unexpected token " + reader.TokenType);
                    reader.Read();
                } while (true);
                return val;
            }

            throw new Exception("Unexpected token " + reader.TokenType);
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }
            List<string> flags = new List<string>();
            var flagValues = Enum.GetValues(value.GetType());
            var names = Enum.GetNames(value.GetType());
            
            var valueNumber = Convert.ToInt64(value);
            int i = 0;
            foreach (var flag in flagValues)
            {
                var flagAsNumber = Convert.ToInt64(flag);
                if (flagAsNumber > 0 && (valueNumber & flagAsNumber) == flagAsNumber)
                {
                    flags.Add(names[i]);
                    valueNumber &=~ flagAsNumber;
                }
                i++;
            }

            writer.WriteValue(string.Join(",", flags));
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
        }
    }
}