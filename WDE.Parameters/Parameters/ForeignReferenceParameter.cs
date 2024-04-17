using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Database;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.Parameters.Parameters;

public class ForeignReferenceParameter : ICustomPickerParameter<long>
{
    private readonly Lazy<ITableEditorPickerService> tableEditorPickerService;
    private readonly DatabaseTable tableName;
    private readonly string columnName;

    public ForeignReferenceParameter(
        Lazy<ITableEditorPickerService> tableEditorPickerService,
        DatabaseTable tableName,
        string columnName)
    {
        this.tableEditorPickerService = tableEditorPickerService;
        this.tableName = tableName;
        this.columnName = columnName;
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.Value.PickByColumn(tableName, default, columnName, value);
        if (result.HasValue)
            return (result.Value, true);
        return (0, false);
    }

    public string? Prefix { get; set; }
    public bool HasItems => true;
    
    public string ToString(long value)
    {
        return value.ToString();
    }

    public Dictionary<long, SelectOption>? Items { get; set; }
}