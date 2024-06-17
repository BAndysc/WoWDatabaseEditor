using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs;

public class DatabaseConditionColumnConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        var cond = (DatabaseConditionColumn)value!;
        if (cond.IsAbs)
            writer.WriteValue("abs(" + cond.Name + ")");
        else
            writer.WriteValue(cond.Name.ToString());
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        Debug.Assert(reader.TokenType == JsonToken.String);
        var value = (string?)reader.Value;

        if (value == null)
            return null;

        if (value.StartsWith("abs("))
            return new DatabaseConditionColumn()
            {
                IsAbs = true,
                Name = ColumnFullName.Parse(value.Substring(4, value.Length - 5))
            };

        return new DatabaseConditionColumn()
        {
            Name = ColumnFullName.Parse(value)
        };
    }

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(DatabaseConditionColumn);
    }
}