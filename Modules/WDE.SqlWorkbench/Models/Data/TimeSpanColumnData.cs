using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.Models;

internal class TimeSpanColumnData : IColumnData
{
    private readonly List<TimeSpan> data = new ();
    private readonly BitArray nulls = new (0);
    
    public bool HasRow(int rowIndex) => rowIndex < data.Count;
        
    private static readonly Regex TimeRegex = new Regex(@"^(-)?(\d+):(\d{2}):(\d{2})(?:\.(\d+))?$");

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
        return timeSpan.ToPrettyString();
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public TimeSpan this[int index] => data[index];
    
    public void Override(int rowIndex, TimeSpan? value)
    {
        nulls[rowIndex] = !value.HasValue;
        data[rowIndex] = value ?? default;
    }
    
    public ColumnTypeCategory Category => ColumnTypeCategory.DateTime;

    public static bool TryParseTime(string str, out TimeSpan ts)
    {
        if (TimeRegex.Match(str) is { Success: true } m)
        {
            var negative = m.Groups[1].Success;
            var hours = int.Parse(m.Groups[2].Value);
            var minutes = int.Parse(m.Groups[3].Value);
            var seconds = int.Parse(m.Groups[4].Value);
            var microSeconds = m.Groups[5].Success ? int.Parse(m.Groups[5].Value.PadRight(6, '0')) : 0;
            if (negative)
            {
                hours = -hours;
                minutes = -minutes;
                seconds = -seconds;
                microSeconds = -microSeconds;
            }
            ts = new TimeSpan(0, hours, minutes, seconds, 0, microSeconds);
            return true;
        }
        ts = default;
        return false;
    }
    
    public bool TryOverride(int rowIndex, string? str, out string? error)
    {
        if (str == null)
        {
            Override(rowIndex, null);
            error = null;
            return true;
        }

        if (TryParseTime(str, out var time))
        {
            Override(rowIndex, time);
            error = null;
            return true;
        }
        
        error = "Invalid time format";
        return false;
    }
    
    public IColumnData CloneEmpty() => new TimeSpanColumnData();
}