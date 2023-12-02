using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services;

[UniqueProvider]
internal interface IExtendedSqlEditorService : ISqlEditorService
{
    void NewDocument();
    void NewDocument(IConnection connection);
    void NewDocumentWithTableInfo(IConnection connection, string schema, string tableName);
    void NewDocumentWithTableSelect(IConnection connection, string schema, string tableName);
    void NewDocumentWithQueryAndExecute(IConnection connection, string query);
    void NewDocumentWithQuery(IConnection connection, string query);
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

    public void NewDocument()
    {
        if (connectionsManager.DefaultConnection != null)
            NewDocument(connectionsManager.DefaultConnection);
        else
            messageBoxService.SimpleDialog("Error", "Can't open an SQL document", "No default connection set. Please set it in SQL Editor settings.").ListenErrors();
    }

    public void NewDocument(IConnection connection)
    {
        var doc = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection));
        doc.Title = $"New query @ {connection.ConnectionData.ConnectionName}";
        documentManager.OpenDocument(doc);
    }

    public void NewDocumentWithTableInfo(IConnection connection, string schema, string tableName)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection));
        vm.Title = $"{tableName} @ {schema}";
        vm.Document.Text = $@"SELECT
    `COLUMN_NAME`,
    `COLUMN_TYPE`,
    `IS_NULLABLE`,
    `COLUMN_KEY`,
    `EXTRA`,
    `COLUMN_DEFAULT`,
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
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection));
        vm.Title = $"{tableName} @ {schema}";
        vm.Document.Text = $"SELECT * FROM `{schema}`.`{tableName}`";
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithQueryAndExecute(IConnection connection, string query)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection));
        vm.Title = $"New query @ {connection.ConnectionData.ConnectionName}";
        vm.Document.Text = query;
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithQuery(IConnection connection, string query)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(IConnection), connection));
        vm.Title = $"New query @ {connection.ConnectionData.ConnectionName}";
        vm.Document.Text = query;
        vm.Document.UndoStack.MarkAsOriginalFile();
        documentManager.OpenDocument(vm);
    }
}