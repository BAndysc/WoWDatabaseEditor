using System;
using System.Text.RegularExpressions;
using Generator.Equals;
using MySqlConnector;

namespace WDE.SqlWorkbench.Models.DataTypes;

[Equatable]
internal readonly partial struct DateDataType
{
    [DefaultEquality]
    public readonly DateTimeDataTypeKind Kind;
    
    [DefaultEquality]
    public readonly int? FractionalSecondsPrecision;
    
    public Type ManagedType
    {
        get
        {
            switch (Kind)
            {
                 case DateTimeDataTypeKind.Date:
                    return typeof(MySqlDateTime);
                case DateTimeDataTypeKind.Time:
                    return typeof(TimeSpan);
                case DateTimeDataTypeKind.DateTime:
                    return typeof(MySqlDateTime);
                case DateTimeDataTypeKind.TimeStamp:
                    return typeof(MySqlDateTime);
                case DateTimeDataTypeKind.Year:
                    return typeof(int);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public override string ToString()
    {
        var type = Kind.ToString().ToUpper();
        if (FractionalSecondsPrecision != null)
            type += $"({FractionalSecondsPrecision})";
        return type;
    }
    
    private static Regex regex = new Regex(@"^(.*?)(?:\s*\(\s*(\d+)\s*\))?$");
    
    public DateDataType(DateTimeDataTypeKind kind) : this(kind, null) { }
    
    private DateDataType(DateTimeDataTypeKind kind, int? fractionalSecondsPrecision)
    {
        Kind = kind;
        FractionalSecondsPrecision = fractionalSecondsPrecision;
    }

    public static bool TryParse(string text, out DateDataType dataType)
    {
        dataType = default;
        var match = regex.Match(text);
        if (!match.Success)
            return false;
        
        DateTimeDataTypeKind? kind = match.Groups[1].Value.ToLower() switch
        {
            "date" => DateTimeDataTypeKind.Date,
            "time" => DateTimeDataTypeKind.Time,
            "datetime" => DateTimeDataTypeKind.DateTime,
            "timestamp" => DateTimeDataTypeKind.TimeStamp,
            "year" => DateTimeDataTypeKind.Year,
            _ => null
        };
        
        if (!kind.HasValue)
            return false;

        int? fsp = null;
        if (match.Groups[2].Success)
        {
            if (!int.TryParse(match.Groups[2].Value, out var len))
                return false;

            fsp = len;
        }
        
        dataType = new DateDataType(kind.Value, fsp);
        
        return true;
    }
}