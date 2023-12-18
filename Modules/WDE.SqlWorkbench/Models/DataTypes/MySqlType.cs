using System;
using Generator.Equals;

namespace WDE.SqlWorkbench.Models.DataTypes;

internal enum MySqlTypeKind
{
    Numeric,
    Text,
    Date,
    Spatial,
    Json
}

[Equatable]
internal readonly partial struct MySqlType
{
    [DefaultEquality]
    public readonly MySqlTypeKind Kind;
    
    [DefaultEquality]
    private readonly NumericDataType numeric;
    
    [DefaultEquality]
    private readonly TextDataType text;
    
    [DefaultEquality]
    private readonly DateDataType date;
    
    [DefaultEquality]
    private readonly SpatialDataType spatial;
    
    [DefaultEquality]
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

    public static bool TryParse(string? text, out MySqlType type)
    {
        if (text == null)
        {
            type = default;
            return false;
        }
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

    public Type ManagedType => Kind switch {
        MySqlTypeKind.Numeric => numeric.ManagedType,
        MySqlTypeKind.Text => text.ManagedType,
        MySqlTypeKind.Date => date.ManagedType,
        MySqlTypeKind.Spatial => spatial.ManagedType,
        MySqlTypeKind.Json => json.ManagedType,
        _ => throw new ArgumentOutOfRangeException()
    };
    
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
    
    public NumericDataType? AsNumeric() => Kind == MySqlTypeKind.Numeric ? numeric : null;
    public TextDataType? AsText() => Kind == MySqlTypeKind.Text ? text : null;
    public DateDataType? AsDate() => Kind == MySqlTypeKind.Date ? date : null;
    public SpatialDataType? AsSpatial() => Kind == MySqlTypeKind.Spatial ? spatial : null;
    public JsonDataType? AsJson() => Kind == MySqlTypeKind.Json ? json : null;
}