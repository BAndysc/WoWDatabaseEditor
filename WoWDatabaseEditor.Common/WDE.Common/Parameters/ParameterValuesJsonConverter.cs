using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;

namespace WDE.Common.Parameters;

public class ParameterValuesJsonConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }

        if (value.GetType() != typeof(Dictionary<long, SelectOption>))
            throw new Exception("Expected type Dictionary<long, SelectOption>");

        var dict = (Dictionary<long, SelectOption>)value;
        
        writer.WriteStartObject();

        var anyHasDescription = dict.Values.Any(v => !string.IsNullOrEmpty(v.Description));
        var useExtendedFormat = anyHasDescription;
        
        foreach (var pair in dict)
        {
            writer.WritePropertyName(pair.Key.ToString());
            if (useExtendedFormat)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("name");
                writer.WriteValue(pair.Value.Name);
                if (pair.Value.Description != null)
                {
                    writer.WritePropertyName("description");
                    writer.WriteValue(pair.Value.Description);
                }
                writer.WriteEndObject();
            }
            else
                writer.WriteValue(pair.Value.Name);
        }
        
        writer.WriteEndObject();
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
                    if (propName != "name" && propName != "description")
                    {
                        throw new Exception("Not expected a key " + propName + " in SelectOption!");
                        // while (reader.TokenType != JsonToken.EndArray)
                        //     reader.Read();
                        // continue;
                    }
                    Debug.Assert(reader.TokenType == JsonToken.String);
                    var propValue = (string?)reader.Value;
                    if (propName == "name")
                        name = propValue!;
                    else if (propName == "description")
                        description = propValue!;
                    else
                        throw new Exception("Not expected a key " + propName + " in SelectOption!");
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