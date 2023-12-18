using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace WDE.SqlWorkbench.Models;

internal class MySqlDateTimeColumnData : IColumnData
{
    private readonly List<MySqlDateTime> data = new ();
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
        data[index] = reader.GetMySqlDateTime(ordinal);
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
        
        var date = data[rowIndex];
        
        if (date.IsValidDateTime)
            return date.GetDateTime().ToString("yyyy-MM-dd HH:mm:ss");
        
        return "0000-00-00";
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public MySqlDateTime this[int index] => data[index];
    
    public void Override(int rowIndex, MySqlDateTime? value)
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

        var datetime = new Regex(@"^(\d{4})-(\d{2})-(\d{2})(?: (\d{2}):(\d{2}):(\d{2})(?:\.(\d+))?)?$");
        var match = datetime.Match(str);
        
        if (match.Success)
        {
            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);
            var hour = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
            var minute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
            var second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;
            var microSecond = match.Groups[7].Success ? int.Parse(match.Groups[7].Value) : 0;
            
            Override(rowIndex, new MySqlDateTime(year, month, day, hour, minute, second, microSecond));
            error = null;
            return true;
        }
        
        error = "Invalid datetime format";
        return false;
    }
    
    public IColumnData CloneEmpty() => new MySqlDateTimeColumnData();
}