using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using WDE.Common;

namespace WDE.SmartScriptEditor.Data;

public enum SmartBuiltInRuleType
{
    Invalid,
    IsRepeat,
    MinMax,
    Custom
}

public readonly struct SmartBuiltInRule
{
    // 0-based index
    public SmartBuiltInRule(SmartBuiltInRuleType type, int param1, int param2 = 0)
    {
        Type = type;
        Parameter1 = param1;
        Parameter2 = param2;
    }

    public SmartBuiltInRuleType Type { get; }
    public int Parameter1 { get; } // 0-based
    public int Parameter2 { get; }

    public override string ToString()
    {
        switch (Type)
        {
            case SmartBuiltInRuleType.Invalid:
                return "(invalid rule)";
            case SmartBuiltInRuleType.IsRepeat:
                return $"is_repeat({Parameter1+1})";
            case SmartBuiltInRuleType.MinMax:
                return $"minmax({Parameter1+1},{Parameter2+1})";
            case SmartBuiltInRuleType.Custom:
                return "custom rule";
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    public static bool TryParse(string str, out SmartBuiltInRule rule)
    {
        rule = default;
        try
        {
            int brace = str.IndexOf("(", StringComparison.Ordinal);
            if (brace == -1 || str.Length == 0 || str[^1] != ')')
                return false;

            string type = str.Substring(0, brace);
            List<int> prams = str
                .Substring(brace + 1, str.Length - brace - 2)
                .Split(',')
                .Select(int.Parse).ToList();

            if (type == "is_repeat")
            {
                if (prams.Count == 1)
                {
                    rule = new SmartBuiltInRule(SmartBuiltInRuleType.IsRepeat, prams[0] - 1);
                    return true;
                }
            }
            else if (type == "minmax")
            {
                if (prams.Count == 2)
                {
                    rule = new SmartBuiltInRule(SmartBuiltInRuleType.MinMax, prams[0] - 1, prams[1] - 1);
                    return true;
                }
            }
        }
        catch (Exception e)
        {
            LOG.LogWarning(e);
            return false;
        }
        return false;
    }
}

public class SmartBuiltinRuleConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is not IList<SmartBuiltInRule> rules)
            throw new Exception("Expected List<SmartBuiltInRule> object");
        writer.WriteStartArray();
        foreach (var rule in rules)
        {
            if (rule.Type == SmartBuiltInRuleType.Invalid)
                throw new Exception("Didn't expect invalid builtin rule");
            writer.WriteValue(rule.ToString());
        }
        writer.WriteEndArray();
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.StartArray)
            throw new Exception("expected array");

        List<SmartBuiltInRule> rules = new();

        while (reader.TokenType != JsonToken.EndArray)
        {
            var stringValue = reader.ReadAsString();
            if (stringValue == null)
                continue;
            if (SmartBuiltInRule.TryParse(stringValue!, out var rule))
                rules.Add(rule);
            else
                throw new Exception("Couldn't parse builtin rule: " + stringValue);    
        }
        return rules;
    }

    public override bool CanConvert(Type objectType)
    {
        return true;
    }
}