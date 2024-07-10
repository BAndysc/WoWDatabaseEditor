using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using Generator.Equals;
using Newtonsoft.Json;

namespace WDE.DatabaseEditors.Data.Structs
{
    [JsonConverter(typeof(ColumnFullNameConverter))]
    [TypeConverter(typeof(ColumnFullNameTypeConverter))]
    public readonly struct ColumnFullName : IEquatable<ColumnFullName>
    {
        public bool Equals(ColumnFullName other)
        {
            return string.Equals(ForeignTable, other.ForeignTable, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(ColumnName, other.ColumnName, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj)
        {
            return obj is ColumnFullName other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ForeignTable?.ToLowerInvariant(), ColumnName.ToLowerInvariant());
        }

        public static bool operator ==(ColumnFullName left, ColumnFullName right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColumnFullName left, ColumnFullName right)
        {
            return !left.Equals(right);
        }

        public readonly string? ForeignTable;
        public readonly string ColumnName;

        public ColumnFullName(string? foreignTable, string columnName)
        {
            ForeignTable = foreignTable;
            ColumnName = columnName;
        }

        public static ColumnFullName Parse(string column)
        {
            var parts = column.Split('.');
            if (parts.Length == 1)
                return new ColumnFullName(null, parts[0]);
            return new ColumnFullName(parts[0], parts[1]);
        }

        public override string ToString()
        {
            if (ForeignTable == null)
                return ColumnName;
            return ForeignTable + "." + ColumnName;
        }
    }

    public class ColumnFullNameTypeConverter : TypeConverter
    {
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string s)
                return ColumnFullName.Parse(s);
            if (value is ColumnFullName columnFullName)
                return columnFullName;
            return null;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || sourceType == typeof(ColumnFullName) || sourceType == typeof(ColumnFullName?);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value is ColumnFullName columnFullName)
                {
                    if (columnFullName.ForeignTable == null)
                        return columnFullName.ColumnName;
                    return columnFullName.ForeignTable + "." + columnFullName.ColumnName;
                }
                else if (value is string s)
                    return s;
                return null;
            }
            else if (destinationType == typeof(ColumnFullName))
            {
                if (value is ColumnFullName columnFullName)
                    return columnFullName;
                if (value is string s)
                    return ColumnFullName.Parse(s);
                if (value is null)
                    return null;
            }

            return null;
        }

        public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
        {
            return destinationType == typeof(string) || destinationType == typeof(ColumnFullName) || destinationType == typeof(ColumnFullName?);
        }
    }

    public class ColumnFullNameConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            if (value is null)
            {
                writer.WriteNull();
            }
            else if (value is ColumnFullName columnFullName)
            {
                if (columnFullName.ForeignTable == null)
                    writer.WriteValue(columnFullName.ColumnName);
                else
                    writer.WriteValue(columnFullName.ForeignTable + "." + columnFullName.ColumnName);
            }
            else
                throw new Exception("Expected ColumnFullName object value.");
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            if (reader.Value is null)
                return null;

            if (reader.Value is string s)
            {
                var parts = s.Split('.');
                if (parts.Length == 1)
                    return new ColumnFullName(null, parts[0]);
                return new ColumnFullName(parts[0], parts[1]);
            }
            throw new Exception("Expected string value.");
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ColumnFullName) || objectType == typeof(ColumnFullName?);
        }
    }

    public class ColumnFullNameIgnoreCaseComparer :
        IComparer<ColumnFullName>,
        IEqualityComparer<ColumnFullName>
    {
        public static ColumnFullNameIgnoreCaseComparer Instance { get; } = new();

        public int Compare(ColumnFullName x, ColumnFullName y)
        {
            if (x.ForeignTable != y.ForeignTable)
                return StringComparer.OrdinalIgnoreCase.Compare(x.ForeignTable, y.ForeignTable);
            return StringComparer.OrdinalIgnoreCase.Compare(x.ColumnName, y.ColumnName);
        }

        public bool Equals(ColumnFullName x, ColumnFullName y)
        {
            return Compare(x, y) == 0;
        }

        public int GetHashCode(ColumnFullName obj)
        {
            return HashCode.Combine(obj.ForeignTable, obj.ColumnName);
        }
    }

    [Equatable]
    public partial class DatabaseColumnJson
    {
        [DefaultEquality]
        [JsonProperty(PropertyName = "name")] 
        public string Name { get; set; } = "";
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "help")] 
        public string? Help { get; set; } = null;
        
        [StringEquality(StringComparison.OrdinalIgnoreCase)]
        [JsonProperty(PropertyName = "db_column_name")]
        [DefaultValue("")]
        public string DbColumnName { get; set; } = "";

        [DefaultEquality]
        [JsonProperty(PropertyName = "column_id")]
        public string? ColumnIdForUi { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "foreign_table")]
        public string? ForeignTable { get; set; }

        [JsonIgnore]
        public ColumnFullName DbColumnFullName => new(ForeignTable, DbColumnName);

        [DefaultEquality]
        [JsonProperty(PropertyName = "value_type")]
        [DefaultValue("")]
        public string ValueType { get; set; } = "";

        [CustomEquality(typeof(DefaultValueEqualityComparer))]
        [JsonProperty(PropertyName = "default")]
        public object? Default { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "autoincrement")]
        public bool AutoIncrement { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "read_only")]
        public bool IsReadOnly { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "can_be_null")]
        public bool CanBeNull { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "is_condition")]
        public bool IsConditionColumn { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "zero_is_blank")]
        public bool IsZeroBlank { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "autogenerate_comment")]
        public string? AutogenerateComment { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "dont_export_autogenerated_comment")]
        public bool DontExportAutogeneratedComment { get; set; }
        
        [DefaultEquality]
        [JsonProperty(PropertyName = "meta")]
        public string? Meta { get; set; }

        [DefaultEquality]
        [JsonProperty(PropertyName = "preferred_width")]
        public float? PreferredWidth { get; set; }

        [IgnoreEquality]
        [JsonIgnore]
        public bool IsMetaColumn => !string.IsNullOrEmpty(Meta);

        [IgnoreEquality]
        [JsonIgnore]
        public bool IsActualDatabaseColumn => !string.IsNullOrEmpty(DbColumnName) && !IsMetaColumn && !IsConditionColumn;
        
        [IgnoreEquality] [JsonIgnore] public bool IsTypeString => ValueType is "string" || ValueType.EndsWith("StringParameter");
        [IgnoreEquality] [JsonIgnore] public bool IsTypeLong => ValueType is "long" or "uint" or "int" || (ValueType.EndsWith("Parameter") && !IsTypeString);
        [IgnoreEquality] [JsonIgnore] public bool IsTypeFloat => ValueType is "float";
        [IgnoreEquality] [JsonIgnore] public bool IsUnixTimestamp => ValueType is "UnixTimestampParameter";
        
        class DefaultValueEqualityComparer : IEqualityComparer<object>
        {
            public static readonly DefaultValueEqualityComparer Default = new();

            public new bool Equals(object? x, object? y)
            {
                if (x == null && y == null)
                    return true;
                if (x == null && y != null || x != null && y == null)
                    return false;
                if (x is string s && y is string s2)
                    return s == s2;
                try
                {
                    var d1 = Convert.ToDouble(x);
                    var d2 = Convert.ToDouble(y);
                    return Math.Abs(d1 - d2) < 0.0001;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            public int GetHashCode(object obj) => 0;
        }
    }
}