using System;
using Prism.Ioc;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Services.MessageBox;
using WDE.Common.Utils;
using WDE.Module.Attributes;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Services;

[UniqueProvider]
internal interface IExtendedSqlEditorService : ISqlEditorService
{
    void NewDocument();
    void NewDocument(DatabaseConnectionData connection);
    void NewDocumentWithTableInfo(DatabaseConnectionData connection, string tableName);
    void NewDocumentWithTableSelect(DatabaseConnectionData connection, string tableName);
    void NewDocumentWithQueryAndExecute(DatabaseConnectionData connection, string query);
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
        if (connectionsManager.DefaultConnection.HasValue)
            NewDocument(connectionsManager.DefaultConnection.Value);
        else
            messageBoxService.SimpleDialog("Error", "Can't open an SQL document", "No default connection set. Please set it in SQL Editor settings.").ListenErrors();
    }

    public void NewDocument(DatabaseConnectionData connection)
    {
        var doc = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(DatabaseConnectionData), connection));
        doc.Title = $"New query @ {connection.ConnectionName}";
        documentManager.OpenDocument(doc);
    }

    public void NewDocumentWithTableInfo(DatabaseConnectionData connection, string tableName)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(DatabaseConnectionData), connection));
        vm.Title = $"`{tableName}` @ {connection.Credentials.SchemaName}";
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
    `TABLE_SCHEMA` = '{connection.Credentials.SchemaName}'
    AND `TABLE_NAME` = '{tableName}'
ORDER BY
    `ORDINAL_POSITION`;";
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithTableSelect(DatabaseConnectionData connection, string tableName)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(DatabaseConnectionData), connection));
        vm.Title = $"`{tableName}` @ {connection.Credentials.SchemaName}";
        vm.Document.Text = $"SELECT * FROM `{tableName}`";
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }

    public void NewDocumentWithQueryAndExecute(DatabaseConnectionData connection, string query)
    {
        var vm = containerProvider.Resolve<SqlWorkbenchViewModel>((typeof(DatabaseConnectionData), connection));
        vm.Title = $"New query @ {connection.Credentials.SchemaName}";
        vm.Document.Text = query;
        vm.Document.UndoStack.MarkAsOriginalFile();
        vm.ExecuteAllCommand.Execute(null);
        documentManager.OpenDocument(vm);
    }
}