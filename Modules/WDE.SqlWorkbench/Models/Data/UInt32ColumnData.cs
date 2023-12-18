using System.Collections;
using System.Collections.Generic;

namespace WDE.SqlWorkbench.Models;

internal class UInt32ColumnData : IColumnData
{
    private readonly List<uint> data = new ();
    private readonly BitArray nulls = new (0);
    
    public bool HasRow(int rowIndex) => rowIndex < data.Count;
        
    public int AppendNull()
    {
        if (data.Count == nulls.Length)
            nulls.Length = nulls.Length * 2 + 1;

        var index = data.Count;
        nulls[index] = true;
        data.Add(default);
        return index;
    }
    
    public void Append(IMySqlDataReader reader, int ordinal)
    {
        var index = AppendNull();

        if (reader.IsDBNull(ordinal))
            return;
        
        nulls[index] = false;
        data[index] = reader.GetUInt32(ordinal);
    }

    public void Clear()
    {
        data.Clear();
        nulls.Length = 0;
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public uint this[int index] => data[index];
    
    public void Override(int rowIndex, uint? value)
    {
        nulls[rowIndex] = !value.HasValue;
        data[rowIndex] = value ?? default;
    }
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Number;
    
    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        if (str == null)
        {
            Override(rowIndex, null);
            error = null;
            return true;
        }

        if (uint.TryParse(str, out var value))
        {
            Override(rowIndex, value);
            error = null;
            return true;
        }
        
        error = "Invalid int value (allowed range: 0 - 4294967295)";
        return false;
    }
    
    public IColumnData CloneEmpty() => new UInt32ColumnData();
}