using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;

namespace WDE.SqlWorkbench.Services.TableUtils;

[AutoRegister]
[SingleInstance]
internal class TableUtility : ITableUtility
{
    private readonly IClipboardService clipboard;
    private readonly IExtendedSqlEditorService editorService;

    public TableUtility(IClipboardService clipboard,
        IExtendedSqlEditorService editorService)
    {
        this.clipboard = clipboard;
        this.editorService = editorService;
    }

    public void OpenSelectRows(in DatabaseConnectionData connection, string schema, string table)
    {
        editorService.NewDocumentWithTableSelect(connection.WithSchemaName(schema), table);
    }

    public void InspectTable(in DatabaseConnectionData connection, string schema, string table)
    {
        editorService.NewDocumentWithTableInfo(connection.WithSchemaName(schema), table);
    }
}