using System.Collections;
using System.Collections.Generic;

namespace WDE.SqlWorkbench.Models;

internal class UInt16ColumnData : IColumnData
{
    private readonly List<ushort> data = new ();
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
        data[index] = reader.GetUInt16(ordinal);
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
    
    public ushort this[int index] => data[index];
    
    public void Override(int rowIndex, ushort? value)
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

        if (ushort.TryParse(str, out var value))
        {
            Override(rowIndex, value);
            error = null;
            return true;
        }
        
        error = "Invalid short value (allowed range: 0 - 65535)";
        return false;
    }
    
    public IColumnData CloneEmpty() => new UInt16ColumnData();
}