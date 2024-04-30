using System.Diagnostics.CodeAnalysis;
using NSubstitute;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.MySqlDatabaseCommon.Providers;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.SqlDump;
using WDE.SqlWorkbench.Services.SqlImport;
using WDE.SqlWorkbench.Services.TablesPanel;
using WDE.SqlWorkbench.Services.TableUtils;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.Test.Mock;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Test.IntegrationTests;

[SuppressMessage("Assertion", "NUnit2005:Consider using Assert.That(actual, Is.EqualTo(expected)) instead of Assert.AreEqual(expected, actual)")]
internal class ConnectionListToolViewModelTests
{
    protected IUserQuestionsService userQuestionsService = null!;
    protected IClipboardService clipboard = null!;
    protected IExtendedSqlEditorService extendedSqlEditorService = null!;
    protected MockSqlConnector connector = null!;
    protected IDatabaseDumpService databaseDumpService = null!;
    protected IDatabaseImportService databaseImportService = null!;
    protected IQueryDialogService queryDialogService = null!;
    protected IMainThread mainThread = null!;
    protected IWorldDatabaseSettingsProvider worldDatabaseSettingsProvider = null!;
    protected IAuthDatabaseSettingsProvider authDatabaseSettingsProvider = null!;
    protected IHotfixDatabaseSettingsProvider hotfixDatabaseSettingsProvider = null!;
    protected ISqlWorkbenchPreferences sqlWorkbenchPreferences = null!;
    protected IConnectionsManager connectionsManager = null!;
    protected MockSqlConnector.MockMemoryServer mockServer = null!;
    protected ManualSynchronizationContext synchronizationContext = null!;
    
    [SetUp]
    public void Init()
    {
        userQuestionsService = Substitute.For<IUserQuestionsService>();
        clipboard = Substitute.For<IClipboardService>();
        extendedSqlEditorService = Substitute.For<IExtendedSqlEditorService>();
        connector = new MockSqlConnector(new QuerySafetyService(userQuestionsService));
        databaseDumpService = Substitute.For<IDatabaseDumpService>();
        databaseImportService = Substitute.For<IDatabaseImportService>();
        queryDialogService = Substitute.For<IQueryDialogService>();
        mainThread = Substitute.For<IMainThread>();
        worldDatabaseSettingsProvider = Substitute.For<IWorldDatabaseSettingsProvider>();
        authDatabaseSettingsProvider = Substitute.For<IAuthDatabaseSettingsProvider>();
        hotfixDatabaseSettingsProvider = Substitute.For<IHotfixDatabaseSettingsProvider>();
        sqlWorkbenchPreferences = Substitute.For<ISqlWorkbenchPreferences>();
        connectionsManager = new ConnectionsManager(worldDatabaseSettingsProvider, hotfixDatabaseSettingsProvider,
            authDatabaseSettingsProvider, sqlWorkbenchPreferences, connector);
        GlobalApplication.InitializeApplication(mainThread, GlobalApplication.AppBackend.Avalonia);
        synchronizationContext = new ManualSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(synchronizationContext);
        mockServer = connector.CreateServer("localhost", "root", "", 3306);
    }

    protected void RunAsyncTasks()
    {
        synchronizationContext.ExecuteAll();
    }

    private ConnectionListToolViewModel CreateViewModel(DatabaseConnectionData connectionData)
    {
        var connection = new Connection(connector, connectionData);

        var vm = new ConnectionListToolViewModel(userQuestionsService,
            new Lazy<ITableUtility>(Substitute.For<ITableUtility>()),
            clipboard,
            new QueryGenerator(connector),
            databaseDumpService,
            databaseImportService,
            queryDialogService,
            connection,
            connectionsManager,
            sqlWorkbenchPreferences,
            new Lazy<IExtendedSqlEditorService>(extendedSqlEditorService));
        vm.ToolOpened();
        return vm;
    }

    protected ConnectionListToolViewModel CreateConnectedViewModel(string[]? visibleSchemas = null)
    {
        var connectionData = new DatabaseConnectionData(Guid.Empty,
            CredentialsSource.Custom,
            new DatabaseCredentials("root", "", "localhost", 3306, "world"),
            "default",
            default,
            false,
            default,
            visibleSchemas,
            QueryExecutionSafety.AskUnlessSelect);
        return CreateViewModel(connectionData);
    }

    [TearDown]
    public void TearDown()
    {
        GlobalApplication.Deinitialize();
    }

    [Test]
    public async Task Test_InvalidConnection()
    {
        mockServer.Kill();
        using var vm = CreateConnectedViewModel();
        await userQuestionsService.ReceivedWithAnyArgs().ConnectionsErrorAsync(default!);
    }

    [Test]
    public async Task Test_IsExpanded()
    {
        using var vm = CreateConnectedViewModel();
        Assert.AreEqual(1, vm.FlatItems.Count);
        Assert.IsFalse(((SchemaViewModel)vm.FlatItems[0]).IsExpanded);
        Assert.AreEqual("information_schema", ((SchemaViewModel)vm.FlatItems[0]).SchemaName);
        ((SchemaViewModel)vm.FlatItems[0]).IsExpanded = true;

        var visible = vm.FlatItems.Where(x => x.IsVisible).Cast<INamedNodeType>().ToList();
        
        Assert.AreEqual(8, visible.Count);
        Assert.AreEqual("Views", visible[1].Name);
        CollectionAssert.AreEquivalent(new[] {"TABLES", "ROUTINES", "ENGINES", "COLLATIONS", "SCHEMATA", "COLUMNS"}, visible.Skip(2).Select(x => x.Name));
        
        await userQuestionsService.DidNotReceiveWithAnyArgs().ConnectionsErrorAsync(default!);
        
        CollectionAssert.AreEqual(new string[]
        {
            "SHOW DATABASES",
            "SELECT `TABLE_SCHEMA`, `TABLE_NAME`, `TABLE_TYPE`, `ENGINE`, `ROW_FORMAT`, `TABLE_COLLATION`, `DATA_LENGTH`, `TABLE_COMMENT` FROM `information_schema`.`TABLES` WHERE `TABLE_SCHEMA` = 'information_schema' ORDER BY `TABLE_NAME`;",
            "SELECT `SPECIFIC_NAME`, `ROUTINE_SCHEMA`, `ROUTINE_TYPE`, `DATA_TYPE`, `DTD_IDENTIFIER`, `ROUTINE_DEFINITION`, `IS_DETERMINISTIC`, `SQL_DATA_ACCESS`, `SECURITY_TYPE`, `CREATED`, `LAST_ALTERED`, `ROUTINE_COMMENT`, `DEFINER` FROM `information_schema`.`routines` WHERE `ROUTINE_SCHEMA` = 'information_schema' ORDER BY `SPECIFIC_NAME`;"
        }, connector.ExecutedQueries);
    }

    [Test]
    public async Task Test_Delete_OnlyOpensDialog()
    {
        var db = mockServer.CreateDatabase("world");
        var table = db.CreateTable("table", TableType.Table, new ColumnInfo("a", "int", false, false, false, null, null, null));
        
        using var vm = CreateConnectedViewModel(new[]{"world"});
        Assert.AreEqual(1, vm.FlatItems.Count);
        Assert.IsFalse(((SchemaViewModel)vm.FlatItems[0]).IsExpanded);
        Assert.AreEqual("world", ((SchemaViewModel)vm.FlatItems[0]).SchemaName);
        ((SchemaViewModel)vm.FlatItems[0]).IsExpanded = true;

        var visible = vm.FlatItems.Where(x => x.IsVisible).Cast<INamedNodeType>().ToList();
        
        Assert.AreEqual(3, visible.Count);
        Assert.AreEqual("Tables", visible[1].Name);
        
        vm.Selected = visible[2];
        
        vm.DropTableCommand.Execute();
        vm.TruncateTableCommand.Execute();
        
        queryDialogService.Received().ShowQueryDialog("DROP TABLE `world`.`table`;");
        queryDialogService.Received().ShowQueryDialog("TRUNCATE TABLE `world`.`table`;");
        
        await userQuestionsService.DidNotReceiveWithAnyArgs().ConnectionsErrorAsync(default!);
        
        CollectionAssert.AreEqual(new string[]
        {
            "SHOW DATABASES",
            "SELECT `TABLE_SCHEMA`, `TABLE_NAME`, `TABLE_TYPE`, `ENGINE`, `ROW_FORMAT`, `TABLE_COLLATION`, `DATA_LENGTH`, `TABLE_COMMENT` FROM `information_schema`.`TABLES` WHERE `TABLE_SCHEMA` = 'world' ORDER BY `TABLE_NAME`;",
            "SELECT `SPECIFIC_NAME`, `ROUTINE_SCHEMA`, `ROUTINE_TYPE`, `DATA_TYPE`, `DTD_IDENTIFIER`, `ROUTINE_DEFINITION`, `IS_DETERMINISTIC`, `SQL_DATA_ACCESS`, `SECURITY_TYPE`, `CREATED`, `LAST_ALTERED`, `ROUTINE_COMMENT`, `DEFINER` FROM `information_schema`.`routines` WHERE `ROUTINE_SCHEMA` = 'world' ORDER BY `SPECIFIC_NAME`;"
        }, connector.ExecutedQueries);
    }
}