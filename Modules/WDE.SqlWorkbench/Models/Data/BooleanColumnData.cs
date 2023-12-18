using System;
using System.Collections;

namespace WDE.SqlWorkbench.Models;

internal class BooleanColumnData : IColumnData
{
    private readonly BitArray data = new (0);
    private readonly BitArray nulls = new (0);
    private int capacity;
    private int count;
    
    public bool HasRow(int rowIndex) => rowIndex < count;

    public int AppendNull()
    {
        if (count == capacity)
        {
            capacity = capacity * 2 + 1;
            nulls.Length = capacity;
            data.Length = capacity;
        }

        var index = count++;
        nulls[index] = true;
        return index;
    }

    public void Append(IMySqlDataReader reader, int ordinal)
    {
        var index = AppendNull();

        if (reader.IsDBNull(ordinal))
            return;
        
        nulls[index] = false;
        data[index] = reader.GetBoolean(ordinal);
    }

    public void Clear()
    {
        count = 0;
        capacity = 0;
        nulls.Length = 0;
        data.Length = 0;
    }

    public string? GetToString(int rowIndex)
    {
        return nulls[rowIndex] ? null : data[rowIndex].ToString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public bool this[int index] => data[index];
    
    public void Override(int rowIndex, bool? value)
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

        if (str.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("1", StringComparison.OrdinalIgnoreCase))
        {
            Override(rowIndex, true);
            error = null;
            return true;
        }

        if (str.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            str.Equals("0", StringComparison.OrdinalIgnoreCase))
        {
            Override(rowIndex, false);
            error = null;
            return true;
        }
        
        error = "Invalid boolean value (allowed values: true/yes/1 or false/no/0)";
        return false;
    }

    public IColumnData CloneEmpty() => new BooleanColumnData();
}
