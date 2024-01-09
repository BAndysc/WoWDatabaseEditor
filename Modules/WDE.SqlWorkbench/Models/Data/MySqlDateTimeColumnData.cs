using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using MySqlConnector;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.Utils;

namespace WDE.SqlWorkbench.Models;

internal class MySqlDateTimeColumnData : IColumnData
{
    private readonly List<MySqlDateTime> data = new ();
    private readonly BitArray nulls = new (0);
    
    public bool HasRow(int rowIndex) => rowIndex < data.Count;

    private bool dateOnly;
    private string? mySqlDataType;
    
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

        return date.ToPrettyString(dateOnly);
    }

    public bool IsNull(int rowIndex) => nulls[rowIndex];
    
    public MySqlDateTime this[int index] => data[index];
    
    public void Override(int rowIndex, MySqlDateTime? value)
    {
        nulls[rowIndex] = !value.HasValue;
        data[rowIndex] = value ?? default;
    }
    
    public ColumnTypeCategory Category => ColumnTypeCategory.DateTime;
    
    private static readonly Regex DateTimeRegex = new Regex(@"^(\d{4})-(\d{2})-(\d{2})(?: +(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d+))?)?)?$");

    public MySqlDateTimeColumnData(string? mySqlDataType)
    {
        this.mySqlDataType = mySqlDataType;
        if (mySqlDataType != null && MySqlType.TryParse(mySqlDataType, out var type) && type.Kind == MySqlTypeKind.Date)
        {
            dateOnly = type.AsDate()!.Value.Kind == DateTimeDataTypeKind.Date;
        }
    }

    public static bool TryParse(string str, out MySqlDateTime dateTime)
    {
        if (DateTimeRegex.Match(str) is {Success: true} match)
        {
            var year = int.Parse(match.Groups[1].Value);
            var month = int.Parse(match.Groups[2].Value);
            var day = int.Parse(match.Groups[3].Value);
            var hour = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 0;
            var minute = match.Groups[5].Success ? int.Parse(match.Groups[5].Value) : 0;
            var second = match.Groups[6].Success ? int.Parse(match.Groups[6].Value) : 0;
            var microSecond = match.Groups[7].Success ? int.Parse(match.Groups[7].Value.PadRight(6, '0')) : 0;
            
            dateTime = new MySqlDateTime(year, month, day, hour, minute, second, microSecond);
            return true;
        }

        dateTime = default;
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

        if (TryParse(str, out var dateTime))
        {
            Override(rowIndex, dateTime);
            error = null;
            return true;
        }

        error = "Invalid datetime format: YYYY-MM-DD HH:MM:SS";
        return false;
    }
    
    public IColumnData CloneEmpty() => new MySqlDateTimeColumnData(mySqlDataType);
}