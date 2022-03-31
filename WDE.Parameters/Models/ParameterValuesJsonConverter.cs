using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Newtonsoft.Json;
using WDE.Common.Parameters;

namespace WDE.Parameters.Models;

public class ParameterValuesJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    // Supports following formats:
    // Dict: "[VALUE]": {"name": "[NAME]"}
    // Dict: "[VALUE]": {"name": "[NAME]", "description": "[DESC]"}
    // Dict: "[VALUE]": "NAME"
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        Dictionary<long, SelectOption> options = new();
        Debug.Assert(reader.TokenType == JsonToken.StartObject);
        reader.Read();
        do
        {
            if (reader.TokenType == JsonToken.EndObject)
                break;
            while (reader.TokenType == JsonToken.Comment)
                reader.Read();
            if (reader.TokenType == JsonToken.EndObject)
                break;
            Debug.Assert(reader.TokenType == JsonToken.PropertyName);
            var key = (string?)reader.Value;
            Debug.Assert(key != null);

            long keyLong = 0;
            if (key.StartsWith("0x"))
                keyLong = long.Parse(key.AsSpan(2), NumberStyles.HexNumber);
            else
                keyLong = long.Parse(key, NumberStyles.Number);
            reader.Read();
            if (reader.TokenType == JsonToken.String)
            {
                var value = (string?)reader.Value;
                Debug.Assert(value != null);
                options[keyLong] = new SelectOption(value);
            }
            else if (reader.TokenType == JsonToken.StartObject)
            {
                string? name = null;
                string? description = null;

                while (true)
                {
                    reader.Read();
                    if (reader.TokenType == JsonToken.EndObject)
                        break;
                    Debug.Assert(reader.TokenType == JsonToken.PropertyName);
                    var propName = (string?)reader.Value;
                    reader.Read();
                    Debug.Assert(reader.TokenType == JsonToken.String);
                    var propValue = (string?)reader.Value;
                    if (propName == "name")
                        name = propValue!;
                    else if (propName == "description")
                        description = propValue!;
                }

                Debug.Assert(name != null);
                options[keyLong] = new SelectOption(name, description);
            }
            else
            {
                throw new Exception("Invalid JSON at values: {} key");
            }
        } while (reader.Read());
            
        Debug.Assert(reader.TokenType == JsonToken.EndObject);

        return options;
    }

    public override bool CanConvert(Type objectType)
    {
        throw new NotImplementedException();
    }
}