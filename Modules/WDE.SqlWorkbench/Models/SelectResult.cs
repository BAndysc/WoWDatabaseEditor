using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using MySqlConnector;

namespace WDE.SqlWorkbench.Models;

internal readonly struct SelectResult
{
    public readonly string[] ColumnNames;
    public readonly Type?[] ColumnTypes;
    public readonly IColumnData?[] Columns;
    public readonly int AffectedRows;

    public readonly bool IsNonQuery;
    
    private SelectResult(string[]? columnNames, Type?[]? columnTypes, IColumnData[]? columns, int affectedRows = -1, bool nonQuery = false)
    {
        ColumnNames = columnNames ?? Array.Empty<string>();
        ColumnTypes = columnTypes ?? Array.Empty<Type>();
        Columns = columns ?? Array.Empty<IColumnData>();
        AffectedRows = affectedRows;
        IsNonQuery = nonQuery;
    }

    public IColumnData? this[string columnName]
    {
        get
        {
            var index = Array.IndexOf(ColumnNames, columnName);
            if (index == -1)
                throw new Exception($"Column {columnName} not found");
            return Columns[index];
        }
    }
    
    public static SelectResult NonQuery(int affectedRows)
    {
        return new SelectResult(null, null, null, affectedRows, true);
    }
    
    public static SelectResult Query(string[] columnNames, Type?[] columnTypes, int rows, IColumnData[] columns)
    {
        return new SelectResult(columnNames, columnTypes, columns, rows, false);
    }
}

internal enum ColumnTypeCategory
{
    Unknown,
    String,
    Number,
    DateTime,
}

internal interface IColumnData
{
    void Append(MySqlDataReader reader, int ordinal);
    string? GetToString(int rowIndex);
    bool IsNull(int rowIndex);
    ColumnTypeCategory Category { get; }
}

internal class ObjectColumnData : IColumnData
{
    private List<object?> data = new List<object?>();
    
    public ObjectColumnData()
    {
    }

    public void Append(MySqlDataReader reader, int ordinal)
    {
        data.Add(reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return data[rowIndex]?.ToString();
    }

    public bool IsNull(int rowIndex) => data[rowIndex] == null;
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;

    public object? this[int index] => data[index];
}

internal class StringColumnData : IColumnData
{
    private List<string?> data = new List<string?>();
    
    public StringColumnData()
    {
    }

    public void Append(MySqlDataReader reader, int ordinal)
    {
        data.Add(reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return data[rowIndex];
    }

    public bool IsNull(int rowIndex) => data[rowIndex] == null;
    
    public string? this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.String;
}

internal class BooleanColumnData : IColumnData
{
    private readonly BitArray data = new (0);
    private readonly BitArray nulls = new (0);
    private int capacity;
    private int count;
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (count == capacity)
        {
            capacity = capacity * 2 + 1;
            nulls.Length = capacity;
            data.Length = capacity;
        }

        if (reader.IsDBNull(ordinal))
            nulls[count++] = true;
        else
            data[count++] = reader.GetBoolean(ordinal);
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public bool this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class ByteColumnData : IColumnData
{
    private readonly List<byte> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetByte(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public byte this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class SByteColumnData : IColumnData
{
    private readonly List<sbyte> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetSByte(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public sbyte this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class Int16ColumnData : IColumnData
{
    private readonly List<short> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetInt16(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public short this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class UInt16ColumnData : IColumnData
{
    private readonly List<ushort> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetUInt16(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public ushort this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class Int32ColumnData : IColumnData
{
    private readonly List<int> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetInt32(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public int this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class UInt32ColumnData : IColumnData
{
    private readonly List<uint> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetUInt32(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public uint this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class Int64ColumnData : IColumnData
{
    private readonly List<long> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetInt64(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public long this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class UInt64ColumnData : IColumnData
{
    private readonly List<ulong> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetUInt64(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public ulong this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class CharColumnData : IColumnData
{
    private readonly List<char> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetChar(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public char this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;
}

internal class DecimalColumnData : IColumnData
{
    private readonly List<decimal> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetDecimal(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public decimal this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class DoubleColumnData : IColumnData
{
    private readonly List<double> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetDouble(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public double this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class FloatColumnData : IColumnData
{
    private readonly List<float> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetFloat(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public float this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
}

internal class DateTimeColumnData : IColumnData
{
    private readonly List<DateTime> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetDateTime(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public DateTime this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.DateTime;
}

internal class DateTimeOffsetColumnData : IColumnData
{
    private readonly List<DateTimeOffset> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetDateTimeOffset(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public DateTimeOffset this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;
}

internal class GuidColumnData : IColumnData
{
    private readonly List<Guid> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetGuid(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public Guid this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;
}

internal class TimeSpanColumnData : IColumnData
{
    private readonly List<TimeSpan> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetTimeSpan(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public TimeSpan this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;
}

internal class BinaryColumnData : IColumnData
{
    private readonly Dictionary<int, string> cachedStrings = new ();
    private readonly List<(int start, int length)> offsets = new List<(int start, int length)>();
    private readonly BitArray nulls = new (0);
    private byte[] data = new byte[0];
    private int bytesCount = 0;
    private int bytesCapacity = 0;
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (offsets.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[offsets.Count] = true;
            offsets.Add(default);
        }
        else
        {
            var length = reader.GetBytes(ordinal, 0, null, 0, 0);
            if (length + bytesCount >= int.MaxValue)
                throw new Exception("Too big binary data!");

            var iLength = (int)length;
            
            if (bytesCapacity < bytesCount + iLength)
            {
                bytesCapacity = Math.Max(bytesCapacity * 2 + 1, bytesCapacity + iLength);
                var newData = new byte[bytesCapacity];
                Array.Copy(data, newData, bytesCount);
                data = newData;
            }

            length = reader.GetBytes(ordinal, 0, data, bytesCount, iLength);
            Debug.Assert(iLength == length);
            
            offsets.Add((bytesCount, iLength));
            bytesCount += iLength;
        }
    }

    public string? GetToString(int rowIndex)
    {
        if (cachedStrings.TryGetValue(rowIndex, out var cached))
            return cached;
        
        var span = this[rowIndex];
        var str = Convert.ToHexString(span.Slice(0, Math.Min(span.Length, 32768)));
        
        if (span.Length > 32768)
            str += "...";
        
        cachedStrings[rowIndex] = str;
        
        return str;
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];

    public ReadOnlySpan<byte> this[int index]
    {
        get
        {
            var offset = offsets[index];
            return data.AsSpan((int)offset.start, offset.length);
        }
    }
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;
}

internal class MySqlDateTimeColumnData : IColumnData
{
    private readonly List<MySqlDateTime> data = new ();
    private readonly BitArray nulls = new (0);
    
    public void Append(MySqlDataReader reader, int ordinal)
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        if (reader.IsDBNull(ordinal))
        {
            nulls[data.Count] = true;
            data.Add(default);
        }
        else
            data.Add(reader.GetMySqlDateTime(ordinal));
    }

    public string? GetToString(int rowIndex)
    {
        if (nulls[rowIndex])
            return null;
        
        var date = data[rowIndex];
        
        if (date.IsValidDateTime)
            return date.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss");
        
        return "0000-00-00";
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public MySqlDateTime this[int index] => data[index];
    
    public ColumnTypeCategory Category => ColumnTypeCategory.DateTime;
}