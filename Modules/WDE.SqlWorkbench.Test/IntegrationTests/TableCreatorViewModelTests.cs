using NSubstitute;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.LanguageServer;
using WDE.SqlWorkbench.Services.QueryConfirmation;
using WDE.SqlWorkbench.Services.QueryUtils;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.Solutions;
using WDE.SqlWorkbench.Test.Mock;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Test.IntegrationTests;

internal class TableCreatorViewModelTests
{
    protected IUserQuestionsService userQuestionsService = null!;
    protected IQuerySafetyService querySafetyService = null!;
    protected MockSqlConnector connector = null!;
    protected IMainThread mainThread = null!;
    protected IWindowManager windowManager = null!;
    protected MockSqlConnector.MockMemoryServer mockServer = null!;
    protected ManualSynchronizationContext synchronizationContext = null!;
    protected IConnectionsManager connectionsManager = null!;
    protected IQueryConfirmationService confirmationService = null!;
    
    [SetUp]
    public void Init()
    {
        mainThread = Substitute.For<IMainThread>();
        new ActionsOutputViewModel(mainThread);
        userQuestionsService = Substitute.For<IUserQuestionsService>();
        querySafetyService = new QuerySafetyService(userQuestionsService);
        connector = new MockSqlConnector(querySafetyService);
        windowManager = Substitute.For<IWindowManager>();
        connectionsManager = Substitute.For<IConnectionsManager>();
        confirmationService = Substitute.For<IQueryConfirmationService>();
        GlobalApplication.InitializeApplication(mainThread, GlobalApplication.AppBackend.Avalonia);

        synchronizationContext = new ManualSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        mockServer = connector.CreateServer("localhost", "root", "", 3306);
    }
    
    protected void RunAsyncTasks()
    {
        synchronizationContext.ExecuteAll();
    }
    
    [TearDown]
    public void TearDown()
    {
        GlobalApplication.Deinitialize();
    }

    protected TableCreatorViewModel CreateViewModel(string schemaName, string? tableName)
    {
        var connectionData = new DatabaseConnectionData(Guid.Empty,
            CredentialsSource.Custom,
            new DatabaseCredentials("root", "", "localhost", 3306, "world"),
            "default",
            default,
            false,
            default,
            default,
            QueryExecutionSafety.AskUnlessSelect);
        var connection = new Connection(connector, connectionData);
        var vm = new TableCreatorViewModel(mainThread, confirmationService, windowManager, connection, new SchemaName(schemaName), tableName);
        return vm;
    }

    // this is a weird MySql requirement that if AutoIncrement column is present in a key, it must be the first column in the key.
    [Test]
    public async Task Bug_OrderPrimaryKeyByAutoIncrement()
    {
        var db = mockServer.CreateDatabase("world");
        db.CreateTable("table", TableType.Table, new ColumnInfo("a", "int", false, false),
            new ColumnInfo("b", "int", false, false));
        var vm = CreateViewModel("world", "table");
        RunAsyncTasks();
        Assert.IsFalse(vm.IsLoading);

        vm.Columns[1].AutoIncrement = true;

        vm.Columns[0].PrimaryKey = true;
        vm.Columns[1].PrimaryKey = true;
        
        vm.SelectedTabIndex = 2;
        Assert.AreEqual("ALTER TABLE `world`.`table`\nCHANGE COLUMN `b` `b` INT CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci NOT NULL AUTO_INCREMENT,\nADD PRIMARY KEY (`b`, `a`)", vm.QueryDocument.Text);
    }
}