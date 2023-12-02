using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;

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

    public void OpenSelectRows(IConnection connection, string schema, string table)
    {
        editorService.NewDocumentWithTableSelect(connection, schema, table);
    }

    public void InspectTable(IConnection connection, string schema, string table)
    {
        editorService.NewDocumentWithTableInfo(connection, schema, table);
    }
}