using System.Collections.Generic;

namespace WDE.SqlWorkbench.Models;

internal class StringColumnData : IColumnData
{
    private List<string?> data = new List<string?>();
    
    public bool HasRow(int rowIndex) => rowIndex < data.Count;
    
    public StringColumnData()
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
        data[index] = reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public void Clear()
    {
        data.Clear();
    }

    public string? GetToString(int rowIndex)
    {
        return data[rowIndex];
    }

    public bool IsNull(int rowIndex) => data[rowIndex] == null;
    
    public string? this[int index] => data[index];
    
    public void Override(int rowIndex, string? value)
    {
        data[rowIndex] = value;
    }
    
    public ColumnTypeCategory Category => ColumnTypeCategory.String;
    
    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        Override(rowIndex, str);
        error = null;
        return true;
    }
    
    public IColumnData CloneEmpty() => new StringColumnData();
}