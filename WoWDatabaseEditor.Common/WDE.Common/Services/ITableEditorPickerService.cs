using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WDE.Common.Database;
using WDE.Common.Exceptions;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITableEditorPickerService
{
    Task<long?> PickByColumn(DatabaseTable table, DatabaseKey? key, string column, long? initialValue, string? backupColumn = null, string? customWhere = null);
    Task ShowTable(DatabaseTable table, string? condition, DatabaseKey? defaultPartialKey = null);
    Task ShowForeignKey1To1(DatabaseTable table, DatabaseKey key);
}

public readonly struct DatabaseKey : IComparable<DatabaseKey>
{
    private readonly long? Key { get; }
    private readonly long? Key2 { get; }
    private readonly long? Key3 { get; }
    private readonly List<long>? moreKeys;

    public bool IsPhantomKey => !Key.HasValue;
    
    public DatabaseKey()
    {
        Key = Key2 = Key3 = null;
        moreKeys = null;
    }
    
    public DatabaseKey(long key)
    {
        Key = key;
        Key2 = null;
        Key3 = null;
        moreKeys = null;
    }
    
    public DatabaseKey(long key, long key2)
    {
        Key = key;
        Key2 = key2;
        Key3 = null;
        moreKeys = null;
    }

    public DatabaseKey(long key, long key2, long key3)
    {
        Key = key;
        Key2 = key2;
        Key3 = key3;
        moreKeys = null;
    }

    public DatabaseKey(long key, long key2, long key3, IEnumerable<long> more)
    {
        Key = key;
        Key2 = key2;
        Key3 = key3;
        moreKeys = more.ToList();
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
    
    public int Count => Key.HasValue ? 1 + (Key2.HasValue ? 1 + (Key3.HasValue ? 1 + (moreKeys?.Count ?? 0) : 0) : 0) : 0;
    public static DatabaseKey PhantomKey => new DatabaseKey();

    public bool Equals(DatabaseKey other)
    {
        // no-key key should be equal to no other key (including other no-key key!)
        if (!Key.HasValue || !other.Key.HasValue)
            return false;
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
        return Key.HasValue ? HashCode.Combine(Key, Key2, Key3) : 0;
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
                return Key!.Value;
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
        var keyComparison = Nullable.Compare(Key, other.Key);
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
        if (!Key.HasValue)
            return "(phantom)";
        if (!Key2.HasValue)
            return Key.ToString()!;
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
        if (!Key.HasValue)
            throw new Exception("No-key key shouldn't be serialized!");
        if (!Key2.HasValue)
            return Key.ToString()!;
        if (!Key3.HasValue)
            return $"({Key}/{Key2})";
        if (moreKeys == null)
            return $"({Key}/{Key2}/{Key3})";
        
        return $"({Key}/{Key2}/{Key3}/{string.Join('/', moreKeys)})";
    }

    public DatabaseKey WithAlso(long nextKey)
    {
        if (!Key.HasValue)
            throw new Exception("No-key key shouldn't be with-alsoed!");
        if (!Key2.HasValue)
            return new DatabaseKey(Key.Value, nextKey);
        if (!Key3.HasValue)
            return new DatabaseKey(Key.Value, Key2.Value, nextKey);
        if (moreKeys == null)
            return new DatabaseKey(Key.Value, Key2.Value, Key3.Value, new[]{nextKey});
        return new  DatabaseKey(Key.Value, Key2.Value, Key3.Value, moreKeys.Concat(new[]{nextKey}));
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

public class UnsupportedTableException : UserException
{
    public UnsupportedTableException(DatabaseTable table) : base($"Table {table} is not supported")
    {
    }
    
    public UnsupportedTableException(DatabaseTable table, string message) : base($"Error with table {table}: {message}")
    {
    }
}
