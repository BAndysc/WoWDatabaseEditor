using System.IO;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Solutions;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services;

[UniqueProvider]
internal interface IExtendedSqlEditorService : ISqlEditorService
{
    void NewDocument(IConnection connection);
    void NewDocumentWithTableInfo(IConnection connection, string schema, string tableName);
    void NewDocumentWithTableSelect(IConnection connection, string schema, string tableName);
    void NewDocumentWithQueryAndExecute(IConnection connection, string query);
    void NewDocumentWithQuery(IConnection connection, string query);
    void NewDocumentWithDatabaseInfo(IConnection connection, string schema);
}

[AutoRegister]
[SingleInstance]
internal class SqlEditorService : IExtendedSqlEditorService
{
    private readonly IDocumentManager documentManager;
    private readonly IConnectionsManager connectionsManager;
    private readonly IContainerProvider containerProvider;
    private readonly IMessageBoxService messageBoxService;

    public SqlEditorService(IDocumentManager documentManager,
        IConnectionsManager connectionsManager,
        IContainerProvider containerProvider,
        IMessageBoxService messageBoxService)
    {
        this.documentManager = documentManager;
        this.connectionsManager = connectionsManager;
        this.containerProvider = containerProvider;
        this.messageBoxService = messageBoxService;
    }

    private QueryDocumentSolutionItem CreateSolutionItem(IConnection connection)
    {
        var fileName = Path.GetRandomFileName();
        return new QueryDocumentSolutionItem(fileName, connection.ConnectionData.Id, true);
    }
    
    public void NewDocument()
    {
        if (connectionsManager.DefaultConnection != null)
            NewDocument(connectionsManager.DefaultConnection);
        else
            messageBoxService.SimpleDialog("Error", "Can't open an SQL document", "No default connection set. Please set it in SQL Editor settings.").ListenErrors();
    }

    public void NewDocument(IConnection connection)
    {
        var doc = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), CreateSolutionItem(connection)));
        doc.Title = $"New query @ {connection.ConnectionData.ConnectionName}";
        documentManager.OpenDocument(doc);
    }

    public void NewDocumentWithTableInfo(IConnection connection, string schema, string tableName)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), CreateSolutionItem(connection)));
        vm.Title = $"{tableName} @ {schema}";
        vm.Document.Text = $@"SELECT
    `COLUMN_NAME`,
    `COLUMN_TYPE`,
    `IS_NULLABLE`,
    `COLUMN_KEY`,
    `EXTRA`,
    `COLUMN_DEFAULT`,
    `CHARACTER_SET_NAME`,
    `COLLATION_NAME`,
    `COLUMN_COMMENT`
FROM
    `information_schema`.`COLUMNS`
WHERE
    `TABLE_SCHEMA` = '{schema}'
    AND `TABLE_NAME` = '{tableName}'
ORDER BY
    `ORDINAL_POSITION`;";
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithTableSelect(IConnection connection, string schema, string tableName)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), CreateSolutionItem(connection)));
        vm.Title = $"{tableName} @ {schema}";
        vm.Document.Text = $"SELECT * FROM `{schema}`.`{tableName}`;";
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithQueryAndExecute(IConnection connection, string query)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), CreateSolutionItem(connection)));
        vm.Title = $"New query @ {connection.ConnectionData.ConnectionName}";
        vm.Document.Text = query;
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithQuery(IConnection connection, string query)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), CreateSolutionItem(connection)));
        vm.Title = $"New query @ {connection.ConnectionData.ConnectionName}";
        vm.Document.Text = query;
        vm.Document.UndoStack.MarkAsOriginalFile();
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithDatabaseInfo(IConnection connection, string schema)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection), (typeof(QueryDocumentSolutionItem), CreateSolutionItem(connection)));
        vm.Title = $"{schema}";
        vm.Document.Text = $@"SELECT
    `TABLE_NAME`,
    `TABLE_TYPE`,
    `ENGINE`,
    `VERSION`,
    `ROW_FORMAT`,
    `TABLE_ROWS`,
    `AUTO_INCREMENT`,
    `TABLE_COLLATION`,
    `CREATE_OPTIONS`,
    `TABLE_COMMENT`,
    `AVG_ROW_LENGTH`,
    `DATA_LENGTH`,
    `MAX_DATA_LENGTH`,
    `INDEX_LENGTH`,
    `CREATE_TIME`,
    `UPDATE_TIME`
FROM
    `information_schema`.`TABLES`
WHERE
    `TABLE_SCHEMA` = '{schema}'
ORDER BY
    `TABLE_NAME`;";
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }
}