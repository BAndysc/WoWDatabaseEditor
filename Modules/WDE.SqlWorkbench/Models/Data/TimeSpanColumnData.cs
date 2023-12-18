using System;
using System.Collections;
using System.Collections.Generic;

namespace WDE.SqlWorkbench.Models;

internal class TimeSpanColumnData : IColumnData
{
    private readonly List<TimeSpan> data = new ();
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
        data[index] = reader.GetTimeSpan(ordinal);
    }

    public void Clear()
    {
        data.Clear();
        nulls.Length = 0;
    }

    public string? GetToString(int rowIndex)
    {
        if (nulls[rowIndex])
            return null;

        var timeSpan = data[rowIndex];
        return $"{(int)timeSpan.TotalHours:00}:{Math.Abs(timeSpan.Minutes):00}:{Math.Abs(timeSpan.Seconds):00}";
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public TimeSpan this[int index] => data[index];
    
    public void Override(int rowIndex, TimeSpan? value)
    {
        nulls[rowIndex] = !value.HasValue;
        data[rowIndex] = value ?? default;
    }
    
    public ColumnTypeCategory Category => ColumnTypeCategory.DateTime;
    
    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        if (str == null)
        {
            Override(rowIndex, null);
            error = null;
            return true;
        }
        
        if (TimeSpan.TryParse(str, out var value))
        {
            Override(rowIndex, value);
            error = null;
            return true;
        }
        
        error = "Invalid time format";
        return false;
    }
    
    public IColumnData CloneEmpty() => new TimeSpanColumnData();
}