using System.Collections.Generic;

namespace WDE.SqlWorkbench.Models;

internal class ObjectColumnData : IColumnData
{
    private List<object?> data = new List<object?>();
    
    public bool HasRow(int rowIndex) => rowIndex < data.Count;
    
    public ObjectColumnData()
    {
    }

    public int AppendNull()
    {
        var index = data.Count;
        data.Add(null);
        return index;
    }
    
    public void Append(IMySqlDataReader reader, int ordinal)
    {
        var index = AppendNull();
        data[index] = reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    public void Clear()
    {
        data.Clear();
    }

    public string? GetToString(int rowIndex)
    {
        return data[rowIndex]?.ToString();
    }

    public bool IsNull(int rowIndex) => data[rowIndex] == null;
    
    public ColumnTypeCategory Category => ColumnTypeCategory.Unknown;
    
    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        error = "Cannot override unknown column type";
        return false;
    }

    public object? this[int index] => data[index];
    
    public void Override(int rowIndex, object? value)
    {
        data[rowIndex] = value;
    }
    
    public IColumnData CloneEmpty() => new ObjectColumnData();
}