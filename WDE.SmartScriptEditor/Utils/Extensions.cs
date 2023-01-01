using System;
using System.Text.RegularExpressions;
using WDE.Common.Database;
using WDE.SmartScriptEditor.Models;

namespace WDE.SmartScriptEditor.Utils;

public static class Extensions
{
    public static bool TryParseGlobalVariable(this string comment, out GlobalVariable variable)
    {
        variable = null!;
        if (!comment.StartsWith("#define"))
            return false;
            
        Match match = Regex.Match(comment, @"#define ([A-Za-z]+) (\d+) (?:entry: (\d+) )?(.*?)(?: -- (.*?))?$", RegexOptions.IgnoreCase);
        if (!match.Success)
            return false;

        if (!Enum.TryParse(typeof(GlobalVariableType), match.Groups[1].Value, out var enm) || enm == null)
            return false;

        if (!long.TryParse(match.Groups[2].Value, out var key))
            return false;

        uint entry = 0;
        if (match.Groups[3].Success && !uint.TryParse(match.Groups[3].Value, out entry))
            return false;

        variable = new GlobalVariable()
        {
            Entry = entry,
            Name = match.Groups[4].Value,
            Comment = match.Groups[5].Value,
            Key = key,
            VariableType = (GlobalVariableType)enm
        };
        return true;
    }
}