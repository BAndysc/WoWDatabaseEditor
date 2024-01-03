using System;
using System.Text.RegularExpressions;
using Generator.Equals;

namespace WDE.SqlWorkbench.Models.DataTypes;

[Equatable]
internal readonly partial struct NumericDataType
{
    [DefaultEquality]
    public readonly NumericDataTypeKind Kind;
    
    [DefaultEquality]
    public readonly int? M; // display width for ints, precision for floats
    
    [DefaultEquality]
    public readonly int? D; // scale for floats
    
    [DefaultEquality]
    public readonly bool Unsigned;
    
    public Type ManagedType
    {
        get
        {
            switch (Kind)
            {
                case NumericDataTypeKind.Bit:
                    return typeof(ulong); // can store up to 64 bit values
                case NumericDataTypeKind.TinyInt:
                    return Unsigned ? typeof(byte) : typeof(sbyte);
                case NumericDataTypeKind.SmallInt:
                    return Unsigned ? typeof(ushort) : typeof(short);
                case NumericDataTypeKind.MediumInt:
                    return Unsigned ? typeof(uint) : typeof(int);
                case NumericDataTypeKind.Int:
                    return Unsigned ? typeof(uint) : typeof(int);
                case NumericDataTypeKind.BigInt:
                    return Unsigned ? typeof(ulong) : typeof(long);
                case NumericDataTypeKind.Decimal:
                    return typeof(PublicMySqlDecimal);
                case NumericDataTypeKind.Float:
                    return typeof(float);
                case NumericDataTypeKind.Double:
                    return typeof(double);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public override string ToString()
    {
        var type = Kind.ToString().ToUpper();
        if (M != null)
        {
            if (D.HasValue)
                type += $"({M},{D})";
            else
                type += $"({M})";
        }
        if (Unsigned)
            type += " UNSIGNED";
        return type;
    }
    
    private static Regex regex = new Regex(@"^(.*?)(?:\s*\(\s*(\d+)(?:\s*,\s*(\d+))?\s*\))?( unsigned)?$", RegexOptions.IgnoreCase);

    public NumericDataType(NumericDataTypeKind kind, bool unsigned) : this(kind, null, null, unsigned) { }
    
    private NumericDataType(NumericDataTypeKind kind, int? m, int? d, bool unsigned)
    {
        Kind = kind;
        M = m;
        D = d;
        Unsigned = unsigned;
    }

    public static bool TryParse(string text, out NumericDataType dataType)
    {
        dataType = default;
        var match = regex.Match(text);
        if (!match.Success)
            return false;
        
        NumericDataTypeKind? kind = match.Groups[1].Value.ToLower() switch
        {
            "bit" => NumericDataTypeKind.Bit,
            "tinyint" => NumericDataTypeKind.TinyInt,
            "bool" => NumericDataTypeKind.TinyInt,
            "boolean" => NumericDataTypeKind.TinyInt,
            "smallint" => NumericDataTypeKind.SmallInt,
            "mediumint" => NumericDataTypeKind.MediumInt,
            "int" => NumericDataTypeKind.Int,
            "integer" => NumericDataTypeKind.Int,
            "bigint" => NumericDataTypeKind.BigInt,
            "decimal" => NumericDataTypeKind.Decimal,
            "dec" => NumericDataTypeKind.Decimal,
            "float" => NumericDataTypeKind.Float,
            "double" => NumericDataTypeKind.Double,
            "double precision" => NumericDataTypeKind.Double,
            _ => null
        };
        
        if (!kind.HasValue)
            return false;

        int? m = null;
        if (match.Groups[2].Success)
        {
            if (!int.TryParse(match.Groups[2].Value, out var len))
                return false;

            m = len;
        }
        
        int? d = null;
        if (match.Groups[3].Success)
        {
            if (!int.TryParse(match.Groups[3].Value, out var len))
                return false;

            d = len;
        }
        
        bool unsigned = match.Groups[4].Success;
        
        dataType = new NumericDataType(kind.Value, m, d, unsigned);
        
        return true;
    }
}