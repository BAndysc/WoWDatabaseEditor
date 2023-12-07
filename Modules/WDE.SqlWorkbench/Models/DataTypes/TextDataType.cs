using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Generator.Equals;

namespace WDE.SqlWorkbench.Models.DataTypes;

[Equatable]
internal readonly partial struct TextDataType
{
    [DefaultEquality]
    public readonly TextDataTypeKind Kind;
    
    [DefaultEquality]
    public readonly int? Length;
    
    [DefaultEquality]
    public readonly string? Charset;
    
    [DefaultEquality]
    public readonly string? Collate;
    
    [OrderedEquality]
    public readonly IReadOnlyList<string>? EnumValues;
    
    public bool Binary => Kind is TextDataTypeKind.Binary or TextDataTypeKind.VarBinary or TextDataTypeKind.Blob or TextDataTypeKind.LongBlob or TextDataTypeKind.MediumBlob or TextDataTypeKind.TinyBlob;

    public static bool KindCanHaveLength(TextDataTypeKind kind) => kind is TextDataTypeKind.Char
        or TextDataTypeKind.VarChar or TextDataTypeKind.Binary or TextDataTypeKind.VarBinary;
    
    public TextDataType(TextDataTypeKind kind, int? length = null) : this(kind, length, null, null, null) { }
    
    private TextDataType(TextDataTypeKind kind, int? length, string? charset, string? collate, IReadOnlyList<string>? enumValues = null)
    {
        Kind = kind;
        Length = length;
        Charset = charset;
        Collate = collate;
        EnumValues = enumValues;
    }
    
    public System.Type ManagedType => Binary ? typeof(byte[]) : typeof(string);
    
    public override string ToString()
    {
        var type = Kind.ToString().ToUpper();
        if (KindCanHaveLength(Kind))
            type += $"({Length ?? 0})";
        if (EnumValues != null)
            type += $"({string.Join(", ", EnumValues.Select(x => $"'{x}'"))})";
        if (Charset != null)
            type += $" CHARSET {Charset}";
        if (Collate != null)
            type += $" COLLATE {Collate}";
        return type;
    }
    
    private static Regex regex = new Regex(@"^(.*?)(?:\s*\(\s*([^)]+)\s*\))?(?:\s*CHARSET\s*(\w+))?(?:\s*COLLATE\s*(\w+))?$", RegexOptions.IgnoreCase);
    
    public static bool TryParse(string text, out TextDataType dataType)
    {
        dataType = default;
        var match = regex.Match(text);
        if (!match.Success)
            return false;
        
        TextDataTypeKind? kind = match.Groups[1].Value.ToLower() switch
        {
            "char" => TextDataTypeKind.Char,
            "varchar" => TextDataTypeKind.VarChar,
            "binary" => TextDataTypeKind.Binary,
            "varbinary" => TextDataTypeKind.VarBinary,
            "tinyblob" => TextDataTypeKind.TinyBlob,
            "tinytext" => TextDataTypeKind.TinyText,
            "blob" => TextDataTypeKind.Blob,
            "text" => TextDataTypeKind.Text,
            "mediumblob" => TextDataTypeKind.MediumBlob,
            "mediumtext" => TextDataTypeKind.MediumText,
            "longblob" => TextDataTypeKind.LongBlob,
            "longtext" => TextDataTypeKind.LongText,
            "enum" => TextDataTypeKind.Enum,
            "set" => TextDataTypeKind.Set,
            _ => null
        };
        
        if (!kind.HasValue)
            return false;

        List<string>? values = null;
        
        int? length = null;
        if (match.Groups[2].Success)
        {
            if (kind is TextDataTypeKind.Enum or TextDataTypeKind.Set)
            {
                values = new List<string>();
                foreach (var value in match.Groups[2].Value.Split(','))
                    values.Add(value.Trim('\''));
            }
            else
            {
                if (!KindCanHaveLength(kind.Value))
                    return false;
                
                if (!int.TryParse(match.Groups[2].Value, out var len))
                    return false;

                length = len;
            }
        }
        
        string? charset = null;
        if (match.Groups[3].Success)
            charset = match.Groups[3].Value;
        
        string? collate = null;
        if (match.Groups[4].Success)
            collate = match.Groups[4].Value;
        
        dataType = new TextDataType(kind.Value, length, charset, collate, values);
        
        return true;
    }
}