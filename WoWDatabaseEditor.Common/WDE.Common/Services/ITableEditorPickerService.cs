using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Services;

[UniqueProvider]
public interface ITableEditorPickerService
{
    Task<long?> PickByColumn(string table, uint key, string column, long? initialValue, string? backupColumn = null);
}

public class UnsupportedTableException : Exception
{
    public UnsupportedTableException(string table) : base($"Table {table} is not supported")
    {
    }
}