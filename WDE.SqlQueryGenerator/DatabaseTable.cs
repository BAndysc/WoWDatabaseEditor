using System;
using System.ComponentModel;
using System.Globalization;

namespace WDE.Common.Database;

[TypeConverter(typeof(DatabaseTableConverter))]
public readonly record struct DatabaseTable(DataDatabaseType Database, string Table)
{
    public readonly DataDatabaseType Database = Database;
    public readonly string Table = Table;

    public static DatabaseTable WorldTable(string name)
    {
        return new DatabaseTable(DataDatabaseType.World, name);
    }

    public static DatabaseTable HotfixTable(string name)
    {
        return new DatabaseTable(DataDatabaseType.Hotfix, name);
    }

    public override string ToString()
    {
        return $"{Database}.{Table}";
    }

    public bool Equals(DatabaseTable other)
    {
        return Database == other.Database && Table == other.Table;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine((int)Database, Table);
    }

    public static bool TryParse(string s, out DatabaseTable table, DataDatabaseType defaultType = DataDatabaseType.World)
    {
        table = default;
        var indexOfDot = s.IndexOf('.');
        if (indexOfDot == -1)
        {
            table = new DatabaseTable(defaultType, s);
            return true;
        }
        else
        {
            var database = s.AsSpan().Slice(0, indexOfDot).Trim();
            var tableName = s.AsSpan().Slice(indexOfDot + 1).Trim();

            DataDatabaseType type;
            if (MemoryExtensions.Equals("world", database, StringComparison.OrdinalIgnoreCase))
                type = DataDatabaseType.World;
            else if (MemoryExtensions.Equals("hotfix", database, StringComparison.OrdinalIgnoreCase))
                type = DataDatabaseType.Hotfix;
            else
            {
                return false;
            }

            table = new DatabaseTable(type, tableName.ToString());
            return true;
        }
    }
    
    public static DatabaseTable Parse(string databaseAndTable, DataDatabaseType defaultType = DataDatabaseType.World)
    {
        if (!TryParse(databaseAndTable, out var db, defaultType))
        {
            throw new FormatException( $"Can't parse `{databaseAndTable}` into 'world.table' or 'hotfix.table' or 'table' format.");
        }

        return db;
    }
}

public class DatabaseTableConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
    {
        return sourceType == typeof(string);
    }

    public override bool CanConvertTo(ITypeDescriptorContext? context, Type? destinationType)
    {
        return destinationType == typeof(string);
    }

    public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
    {
        if (value is not string stringValue)
            throw new NotSupportedException($"Cannot convert {value} to DatabaseTable");

        return DatabaseTable.Parse(stringValue);
    }

    public override object ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
    {
        if (value is not DatabaseTable dt)
            throw new NotSupportedException($"Cannot convert {value} to string because it is not a DatabaseTable");

        return dt.ToString();
    }
}