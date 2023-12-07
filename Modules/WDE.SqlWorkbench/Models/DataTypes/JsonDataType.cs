using System;
using System.Text.RegularExpressions;
using Generator.Equals;

namespace WDE.SqlWorkbench.Models.DataTypes;

[Equatable]
internal readonly partial struct JsonDataType
{
    public override string ToString()
    {
        return "JSON";
    }
    
    public Type ManagedType => typeof(string);
    
    private JsonDataType(string text)
    {
    }

    public static bool TryParse(string text, out JsonDataType dataType)
    {
        dataType = default;
        if (text.ToLower().Trim() == "json")
        {
            dataType = new JsonDataType(text);
            return true;
        }
        return false;
    }
}