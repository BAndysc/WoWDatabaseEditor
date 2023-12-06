using System;

namespace WDE.SqlWorkbench.Models.DataTypes;

internal enum MySqlTypeKind
{
    Numeric,
    Text,
    Date,
    Spatial,
    Json
}

internal readonly struct MySqlType
{
    public readonly MySqlTypeKind Kind;
    private readonly NumericDataType numeric;
    private readonly TextDataType text;
    private readonly DateDataType date;
    private readonly SpatialDataType spatial;
    private readonly JsonDataType json;
    
    private MySqlType(MySqlTypeKind kind, NumericDataType numeric, TextDataType text, DateDataType date, SpatialDataType spatial, JsonDataType json)
    {
        Kind = kind;
        this.numeric = numeric;
        this.text = text;
        this.date = date;
        this.spatial = spatial;
        this.json = json;
    }
    
    public static MySqlType Numeric(NumericDataType numeric) => new(MySqlTypeKind.Numeric, numeric, default, default, default, default);
    
    public static MySqlType Text(TextDataType text) => new(MySqlTypeKind.Text, default, text, default, default, default);
    
    public static MySqlType Date(DateDataType date) => new(MySqlTypeKind.Date, default, default, date, default, default);
    
    public static MySqlType Spatial(SpatialDataType spatial) => new(MySqlTypeKind.Spatial, default, default, default, spatial, default);
    
    public static MySqlType Json(JsonDataType json) => new(MySqlTypeKind.Json, default, default, default, default, json);

    public static bool TryParse(string text, out MySqlType type)
    {
        if (NumericDataType.TryParse(text, out var numeric))
        {
            type = Numeric(numeric);
            return true;
        }
        
        if (TextDataType.TryParse(text, out var textType))
        {
            type = Text(textType);
            return true;
        }
        
        if (DateDataType.TryParse(text, out var date))
        {
            type = Date(date);
            return true;
        }
        
        if (SpatialDataType.TryParse(text, out var spatial))
        {
            type = Spatial(spatial);
            return true;
        }
        
        if (JsonDataType.TryParse(text, out var json))
        {
            type = Json(json);
            return true;
        }
        
        type = default;
        return false;
    }

    public override string ToString()
    {
        return Kind switch {
            MySqlTypeKind.Numeric => numeric.ToString(),
            MySqlTypeKind.Text => text.ToString(),
            MySqlTypeKind.Date => date.ToString(),
            MySqlTypeKind.Spatial => spatial.ToString(),
            MySqlTypeKind.Json => json.ToString(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}