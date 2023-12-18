using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace WDE.SqlWorkbench.Models;

internal class BinaryColumnData : IColumnData
{
    private readonly Dictionary<int, string?> cachedStrings = new ();
    private readonly List<(int start, int length)> offsets = new List<(int start, int length)>();
    private readonly BitArray nulls = new (0);
    private byte[] data = new byte[0];
    private int bytesCount = 0;
    private int bytesCapacity = 0;
    
    public const int MaxToStringLength = 32768;

    public bool HasRow(int rowIndex) => rowIndex < offsets.Count;

    // returns offset in data array for length bytes.
    private int AllocBytes(int length)
    {
        if (bytesCapacity < bytesCount + length)
        {
            bytesCapacity = Math.Max(bytesCapacity * 2 + 1, bytesCapacity + length);
            var newData = new byte[bytesCapacity];
            Array.Copy(data, newData, bytesCount);
            data = newData;
        }

        var offset = bytesCount;
        bytesCount += length;
        return offset;
    }
    
    public int AppendNull()
    {
        if (offsets.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        var index = offsets.Count;
        nulls[index] = true;
        offsets.Add(default);
        return index;
    }

    public void Append(IMySqlDataReader reader, int ordinal)
    {
        var index = AppendNull();

        if (reader.IsDBNull(ordinal))
            return;
        
        nulls[index] = false;
        var length = reader.GetBytes(ordinal, 0, null, 0, 0);
        if (length + bytesCount >= int.MaxValue)
            throw new Exception("Too big binary data!");

        Debug.Assert((int)length == length);
        var offset = AllocBytes((int)length);

        length = reader.GetBytes(ordinal, 0, data, offset, (int)length);
        offsets[index] = (offset, (int)length);
    }
    
    public void Clear()
    {
        offsets.Clear();
        nulls.Length = 0;
        bytesCount = 0;
    }
    
    public string? GetToString(int rowIndex)
    {
        if (cachedStrings.TryGetValue(rowIndex, out var cached))
            return cached;

        if (IsNull(rowIndex))
        {
            cachedStrings[rowIndex] = null;
            return null;
        }
        else
        {
            var span = this[rowIndex];
            var str = Convert.ToHexString(span.Slice(0, Math.Min(span.Length, MaxToStringLength)));
        
            if (span.Length > MaxToStringLength)
                str += "...";
        
            cachedStrings[rowIndex] = str;
        
            return str;
        }
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

    public void OverrideNull(int rowIndex, bool isNull)
    {
        nulls[rowIndex] = isNull;
        cachedStrings.Remove(rowIndex);
    }
    
    public void Override(int rowIndex, ReadOnlySpan<byte> bytes)
    {
        var offset = AllocBytes(bytes.Length);
        bytes.CopyTo(data.AsSpan(offset, bytes.Length));
        offsets[rowIndex] = (offset, bytes.Length);
        OverrideNull(rowIndex, false);
    }
    
    public ReadOnlyMemory<byte> AsReadOnlyMemory(int rowIndex)
    {
        var offset = offsets[rowIndex];
        return data.AsMemory(offset.start, offset.length);
    }
    
    public Memory<byte> AsWriteableMemory(int rowIndex)
    {
        var offset = offsets[rowIndex];
        return data.AsMemory(offset.start, offset.length);
    }

    public ColumnTypeCategory Category => ColumnTypeCategory.Binary;
    
    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        if (str == null)
        {
            OverrideNull(rowIndex, true);
            error = null;
            return true;
        }

        if (str.Length % 2 != 0)
        {
            error = "Invalid binary string format (must be even number of characters, because each two chars make one byte in hex format)";
            return false;
        }

        var bytes = new byte[str.Length / 2];
        for (int i = 0; i < bytes.Length; ++i)
        {
            if (!byte.TryParse(str.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out var value))
            {
                error = "Invalid binary string format";
                return false;
            }

            bytes[i] = value;
        }

        Override(rowIndex, bytes);
        error = null;
        return true;
    }

    public IColumnData CloneEmpty() => new BinaryColumnData();
}