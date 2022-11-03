using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs;

public class DatabaseConditionColumnConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        Debug.Assert(reader.TokenType == JsonToken.String);
        var value = (string?)reader.Value;

        if (value == null)
            return null;

        //reader.Read();
        if (value.StartsWith("abs("))
            return new DatabaseConditionColumn()
            {
                IsAbs = true,
                Name = value.Substring(4, value.Length - 5)
            };

        return new DatabaseConditionColumn()
        {
            Name = value
        };
    }

    public override bool CanConvert(Type objectType)
    {
        throw new NotImplementedException();
    }
}