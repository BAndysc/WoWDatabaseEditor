using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services.TableUtils;

[AutoRegister]
[SingleInstance]
internal class TableUtility : ITableUtility
{
    private readonly IClipboardService clipboard;
    private readonly IExtendedSqlEditorService editorService;
    private readonly IDocumentManager documentManager;
    private readonly IContainerProvider containerProvider;

    public TableUtility(IClipboardService clipboard,
        IExtendedSqlEditorService editorService,
        IDocumentManager documentManager,
        IContainerProvider containerProvider)
    {
        this.clipboard = clipboard;
        this.editorService = editorService;
        this.documentManager = documentManager;
        this.containerProvider = containerProvider;
    }

    public void OpenSelectRows(IConnection connection, string schema, string table)
    {
        editorService.NewDocumentWithTableSelect(connection, schema, table);
    }

    public void InspectTable(IConnection connection, string schema, string table)
    {
        editorService.NewDocumentWithTableInfo(connection, schema, table);
    }
    
    public void InspectDatabase(IConnection connection, string schema)
    {
        editorService.NewDocumentWithDatabaseInfo(connection, schema);
    }

    public void AlterTable(IConnection connection, string schema, string? table)
    {
        var document =
            containerProvider.Resolve<TableCreatorViewModel>((typeof(IConnection), connection), (typeof(SchemaName), new SchemaName(schema)), (typeof(string), table));
        documentManager.OpenDocument(document);
    }
}