using System;
using System.Collections.Generic;
using WDE.Common.Services.QueryParser.Models;

namespace WDE.SqlInterpreter.Extensions
{
    public static class StringExtensions
    {
        public static bool IsStringLiteral(this string? str)
        {
            if (str == null)
                return false;
            if (str.StartsWith('`') ||
                str.StartsWith('"') ||
                str.StartsWith('\''))
                return true;
            return false;
        }
    
        public static string? DropQuotes(this string? quotedText)
        {
            if (quotedText == null)
                return null;
            if (quotedText.StartsWith('`') ||
                quotedText.StartsWith('"') ||
                quotedText.StartsWith('\''))
                return quotedText.Substring(1, quotedText.Length - 2);
            return quotedText;
        }
    
        public static object? ToType(this string? str, Dictionary<string, object> variables)
        {
            if (str == null)
                return null;
            if (str == "NULL")
                return null;
            if (str.StartsWith("@"))
            {
                if (variables.TryGetValue(str, out var variable))
                    return variable;
                return str;
            }
            if (str.IsStringLiteral())
                return str.DropQuotes();
            if (long.TryParse(str, out var lng))
                return lng;
            if (ulong.TryParse(str, out var ulng))
                return ulng;
            if (float.TryParse(str, out var flt))
                return flt;
            if (str.StartsWith("FROM_UNIXTIME(") && long.TryParse(str.Substring(14, str.Length - 15), out var unixTime))
                return unixTime;
            return new UnknownSqlThing(str);
        }
    }
}