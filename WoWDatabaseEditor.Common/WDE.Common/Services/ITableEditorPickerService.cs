using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITableEditorPickerService
{
    Task<long?> PickByColumn(string table, DatabaseKey key, string column, long? initialValue, string? backupColumn = null);
}

public struct DatabaseKey : IComparable<DatabaseKey>
{
    private long Key { get; }
    private long? Key2 { get; }
    private long? Key3 { get; }
    private List<long>? moreKeys;

    public DatabaseKey(long key)
    {
        Key = key;
        Key2 = null;
        Key3 = null;
        moreKeys = null;
    }
    
    public DatabaseKey(IEnumerable<long> keys)
    {
        using var enumerator = keys.GetEnumerator();
        Key2 = null;
        Key3 = null;
        moreKeys = null;
        if (enumerator.MoveNext())
            Key = enumerator.Current;
        else
            throw new ArgumentOutOfRangeException(nameof(keys));
        
        if (enumerator.MoveNext())
            Key2 = enumerator.Current;
        if (enumerator.MoveNext())
            Key3 = enumerator.Current;
        
        while (enumerator.MoveNext())
        {
            moreKeys ??= new List<long>();
            moreKeys.Add(enumerator.Current);
        }
    }
    
    public int Count => 1 + (Key2.HasValue ? 1 : 0) + (Key3.HasValue ? 1 : 0) + (moreKeys?.Count ?? 0);
    
    public bool Equals(DatabaseKey other)
    {
        return Key == other.Key && Key2 == other.Key2 && Key3 == other.Key3 && KeysListEqual(other);
    }

    private bool KeysListEqual(DatabaseKey other)
    {
        if (moreKeys == null && other.moreKeys == null)
            return true;
        if (moreKeys != null && other.moreKeys != null)
        {
            if (moreKeys.Count != other.moreKeys.Count)
                return false;
            for (int i = 0; i < moreKeys.Count; i++)
            {
                if (moreKeys[i] != other.moreKeys[i])
                    return false;
            }

            return true;
        }

        return false;
    }

    public override bool Equals(object? obj)
    {
        return obj is DatabaseKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Key2, Key3);
    }

    public static bool operator ==(DatabaseKey left, DatabaseKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(DatabaseKey left, DatabaseKey right)
    {
        return !left.Equals(right);
    }

    public long this[int index]
    {
        get
        {
            if (index == 0)
                return Key;
            if (index == 1)
                return Key2!.Value;
            if (index == 2)
                return Key3!.Value;
            if (index > 2)
                return moreKeys![index - 3];
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public int CompareTo(DatabaseKey other)
    {
        var keyComparison = Key.CompareTo(other.Key);
        if (keyComparison != 0) return keyComparison;
        var key2Comparison = Nullable.Compare(Key2, other.Key2);
        if (key2Comparison != 0) return key2Comparison;
        return Nullable.Compare(Key3, other.Key3);
    }

    public static bool operator <(DatabaseKey left, DatabaseKey right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator >(DatabaseKey left, DatabaseKey right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator <=(DatabaseKey left, DatabaseKey right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >=(DatabaseKey left, DatabaseKey right)
    {
        return left.CompareTo(right) >= 0;
    }

    public override string ToString()
    {
        if (!Key2.HasValue)
            return Key.ToString();
        if (!Key3.HasValue)
            return $"({Key}, {Key2})";
        if (moreKeys == null)
            return $"({Key}, {Key2}, {Key3})";
        
        return $"({Key}, {Key2}, {Key3}, {string.Join(", ", moreKeys)})";
    }

    public static bool TryDeserialize(string s, out DatabaseKey key)
    {
        key = default;
        if (s.StartsWith('('))
        {
            key = new DatabaseKey(s.Substring(1, s.Length - 2).Split('/').Select(long.Parse));
            return true;
        }
        else if (long.TryParse(s, out var longVal))
        {
            key = new DatabaseKey(longVal);
            return true;
        }

        return false;
    }
    
    public static DatabaseKey Deserialize(string s)
    {
        if (TryDeserialize(s, out var key))
            return key;
        throw new ArgumentException($"{s} is not a valid key");
    }
    
    public readonly string Serialize()
    {
        if (!Key2.HasValue)
            return Key.ToString();
        if (!Key3.HasValue)
            return $"({Key}/{Key2})";
        if (moreKeys == null)
            return $"({Key}/{Key2}/{Key3})";
        
        return $"({Key}/{Key2}/{Key3}/{string.Join('/', moreKeys)})";
    }
}


public class DatabaseKeyConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof (DatabaseKey) || objectType == typeof (DatabaseKey?);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteValue("");
            return;
        }
        var key = (DatabaseKey) value;
        writer.WriteValue(key.Serialize());
    }

    public override object ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType != JsonToken.Integer && reader.TokenType != JsonToken.String)
            throw new InvalidDataException("A DatabaseKey must be either a number or slash separated values wrapped in parenthesis, e.g. (1/2/3)");
        
        if (reader.TokenType == JsonToken.Integer)
        {
            return new DatabaseKey((long)reader.Value!);
        }
        else // if (reader.TokenType == JsonToken.String)
        {
            return DatabaseKey.Deserialize((string?)reader.Value!);
        }
    }
}

public class UnsupportedTableException : Exception
{
    public UnsupportedTableException(string table) : base($"Table {table} is not supported")
    {
    }
}
