using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Parameters;
using WDE.Common.Services;

namespace WDE.Parameters.Parameters;

public class ForeignReferenceParameter : ICustomPickerParameter<long>
{
    private readonly ITableEditorPickerService tableEditorPickerService;
    private readonly string tableName;
    private readonly string columnName;

    public ForeignReferenceParameter(
        ITableEditorPickerService tableEditorPickerService,
        string tableName,
        string columnName)
    {
        this.tableEditorPickerService = tableEditorPickerService;
        this.tableName = tableName;
        this.columnName = columnName;
    }
    
    public async Task<(long, bool)> PickValue(long value)
    {
        var result = await tableEditorPickerService.PickByColumn(tableName, default, columnName, value);
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