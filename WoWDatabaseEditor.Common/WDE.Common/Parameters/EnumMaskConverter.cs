using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace WDE.Common.Parameters;

public class EnumMaskConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        
        var enumType = GetEnumType(value.GetType());
        var enumValue = Convert.ToUInt64(value);
        var values = Enum.GetValues(enumType);
        var names = Enum.GetNames(enumType);

        List<string> namesList = new();
        string? thisOneMissingEnum = null;
        
        for (int i = values.Length - 1; i >= 0; --i)
        {
            var v = Convert.ToUInt64(values.GetValue(i));
            if (v == 0)
                continue;
            
            if ((enumValue & v) == v)
            {
                namesList.Add(names[i]);
                enumValue &= ~v;
            }
            else if ((enumValue & v) == 0)
            {
                thisOneMissingEnum = names[i];
            }
        }
        if (enumValue > 0)
            namesList.Add(enumValue.ToString());

        if (namesList.Count == values.Length - 1 && thisOneMissingEnum != null) // all but one
            writer.WriteValue($"~{thisOneMissingEnum}");
        else
        {
            namesList.Reverse();
            writer.WriteValue(string.Join(",", namesList));
        }
    }

    private Type GetEnumType(Type type)
    {
        if (type.IsEnum)
            return type;
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            return GetEnumType(type.GetGenericArguments()[0]);
        else
            throw new ArgumentException("Type is not enum or nullable enum", nameof(type));
    }

    private ulong GetEnumValue(Type enumType, ReadOnlySpan<char> text)
    {
        if (text[0] == '~')
        {
            var value = Convert.ToUInt64(Enum.Parse(enumType, text.Slice(1), true));
            ulong allowedValues = 0;
            var possibleValues = Enum.GetValues(enumType);
            foreach (var v in possibleValues)
                allowedValues |= Convert.ToUInt64(v);
            return (~value) & allowedValues;
        }
        else
        {
            return Convert.ToUInt64(Enum.Parse(enumType, text, true));
        }
    }

    private ulong GetEnumValue(Type enumType, string text)
    {
        var value = 0UL;
        if (text.Contains(','))
        {
            int start = 0;
            int end = text.IndexOf(',');
            while (start < text.Length)
            {
                value |= GetEnumValue(enumType, text.AsSpan(start, end - start));
                start = end + 1;
                if (start >= text.Length)
                    break;
                end = text.IndexOf(',', start);
                if (end == -1)
                    end = text.Length;
            }
        }
        else
        {
            value = GetEnumValue(enumType, text.AsSpan());
        }

        return value;
    }
    
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var enumType = GetEnumType(objectType);

        if (reader.TokenType == JsonToken.Integer)
        {
            var value = (long)reader.Value!;
            return Enum.ToObject(enumType, value);
        }
        else if (reader.TokenType == JsonToken.String)
        {
            var value = GetEnumValue(enumType, (string)reader.Value!);
            return Enum.ToObject(enumType, value);
        }
        else if (reader.TokenType == JsonToken.StartArray)
        {
            var value = 0UL;
            do
            {
                reader.Read();
                if (reader.TokenType == JsonToken.String)
                {
                    value |= GetEnumValue(enumType, (string)reader.Value!);
                }
            } while (reader.TokenType != JsonToken.EndArray);

            return Enum.ToObject(enumType, value);
        }
        
        throw new JsonException($"Unexpected token {reader.TokenType} when parsing enum mask");
    }

    public override bool CanConvert(Type objectType)
    {
        throw new NotImplementedException();
    }
}