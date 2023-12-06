using System.Text.RegularExpressions;

namespace WDE.SqlWorkbench.Models.DataTypes;

internal readonly struct JsonDataType
{
    public override string ToString()
    {
        return "JSON";
    }
    
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