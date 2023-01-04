using System.Diagnostics.CodeAnalysis;
using MySqlConnector;
using NSubstitute;
using WDE.Common.Managers;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.Common.Utils;
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

internal class SqlWorkbenchViewModelTests
{
    protected ActionsOutputViewModel actionsOutputService = null!;
    protected ISqlLanguageServer languageServer = null!;
    protected IConfigureService configuration = null!;
    protected QueryUtility queryUtility = null!;
    protected IUserQuestionsService userQuestionsService = null!;
    protected ISqlWorkbenchPreferences preferences = null!;
    protected IClipboardService clipboard = null!;
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
        actionsOutputService = new ActionsOutputViewModel(mainThread);
        languageServer = Substitute.For<ISqlLanguageServer>();
        configuration = Substitute.For<IConfigureService>();
        queryUtility = new QueryUtility();
        userQuestionsService = Substitute.For<IUserQuestionsService>();
        querySafetyService = new QuerySafetyService(userQuestionsService);
        preferences = Substitute.For<ISqlWorkbenchPreferences>();
        clipboard = Substitute.For<IClipboardService>();
        connector = new MockSqlConnector(querySafetyService);
        windowManager = Substitute.For<IWindowManager>();
        connectionsManager = Substitute.For<IConnectionsManager>();
        confirmationService = Substitute.For<IQueryConfirmationService>();
        GlobalApplication.InitializeApplication(mainThread, GlobalApplication.AppBackend.UnitTests);

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
        actionsOutputService.Dispose();
        GlobalApplication.Deinitialize();
    }

    protected SqlWorkbenchViewModel CreateViewModel(DatabaseConnectionData connectionData)
    {
        var connection = new Connection(connector, connectionData);
        var solutionItem = new QueryDocumentSolutionItem("test", connectionData.Id, true);
        var vm = new SqlWorkbenchViewModel(actionsOutputService, languageServer, configuration, queryUtility, userQuestionsService, preferences, clipboard, mainThread, windowManager, connectionsManager, confirmationService, connection, solutionItem);
        return vm;
    }
    
    protected SqlWorkbenchViewModel CreateConnectedViewModel()
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
        return CreateViewModel(connectionData);
    }
    
    [Test]
    public async Task TestNoConnection()
    {
        using var vm = CreateViewModel(default);
        await userQuestionsService.Received().ConnectionsErrorAsync(Arg.Any<MySqlException>());
    }
    
    [Test]
    public async Task Test_ShowTables_CantEdit()
    {
        using var vm = CreateConnectedViewModel();
        vm.Document.Insert(0, "SHOW TABLES");
        await vm.ExecuteAllCommand.ExecuteAsync();
        
        Assert.AreEqual("Error: Unknown database 'world'", actionsOutputService.Actions[0].Response);
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "varchar", false, true, true));

        await vm.ExecuteAllCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[1].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        var results = vm.Results[0];
        Assert.IsTrue(vm.Results[0] is not SelectSingleTableViewModel);
        Assert.AreEqual(2, results.Columns.Count);
        CollectionAssert.AreEqual(new string[]{"#", "Tables_in_world"}, results.Columns.Select(x => x.Header));
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(null, results.GetShortValue(0, 0));
        Assert.AreEqual("tab", results.GetShortValue(0, 1));
        
        results.UpdateSelectedCells("abc");
        await userQuestionsService.Received().InformCantEditNonSelectAsync();
        
        CollectionAssert.AreEqual(new []
        {
            "SHOW TABLES",
            "SHOW TABLES",
            "SHOW FULL TABLES;"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_CantEditSelectWithNoPrimaryKey()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        table.Insert(5, "text");
        
        vm.Document.Insert(0, "SELECT `b` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        var results = vm.Results[0];
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        Assert.AreEqual(2, results.Columns.Count);
        CollectionAssert.AreEqual(new []{"#", "b"}, results.Columns.Select(x => x.Header));
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(null, results.GetShortValue(0, 0));
        Assert.AreEqual("text", results.GetShortValue(0, 1));

        results.SelectedCellIndex = 1;
        results.Selection.Add(0);
        results.UpdateSelectedCells("newText");
        await userQuestionsService.Received().NoFullPrimaryKeyAsync();
        Assert.AreEqual("text", results.GetShortValue(0, 1));
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_CanEditSelectWithPrimaryKey()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        table.Insert(5, "text");
        
        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        Assert.AreEqual(2, results.Columns.Count);
        CollectionAssert.AreEqual(new []{"#", "a"}, results.Columns.Select(x => x.Header));
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(null, results.GetShortValue(0, 0));
        Assert.AreEqual("5", results.GetShortValue(0, 1));
        
        results.SelectedCellIndex = 1;
        results.Selection.Add(0);
        results.UpdateSelectedCells("3");
        Assert.AreEqual("3", results.GetShortValue(0, 1));
        Assert.IsTrue(results.IsModified);
        Assert.IsTrue(vm.IsModified);

        confirmationService.QueryConfirmationAsync(default!, default!).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync("START TRANSACTION").Returns(true);
        userQuestionsService.ConfirmExecuteQueryAsync("COMMIT").Returns(true);
        userQuestionsService.ConfirmExecuteQueryAsync("UPDATE `tab` SET `a` = 3 WHERE `a` = 5").Returns(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[2].IsSuccess);
        
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("COMMIT");
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("UPDATE `tab` SET `a` = 3 WHERE `a` = 5");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            "UPDATE `tab` SET `a` = 3 WHERE `a` = 5",
            "COMMIT",
            "SELECT `a` FROM `tab`",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }

    [Test]
    public async Task Test_CanInsertRows()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        Assert.AreEqual(3, results.Columns.Count);
        CollectionAssert.AreEqual(new []{"#", "b", "a"}, results.Columns.Select(x => x.Header));
        Assert.IsFalse(results.IsColumnPrimaryKey(0));
        Assert.IsFalse(results.IsColumnPrimaryKey(1));
        Assert.IsTrue(results.IsColumnPrimaryKey(2));
        Assert.AreEqual(0, results.Count);
        
        results.AddRowCommand.Execute();
        results.AddRowCommand.Execute();
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("txt");
        results.SelectedCellIndex = 2;
        results.UpdateSelectedCells("3");

        Assert.IsTrue(results.IsModified);
        Assert.IsTrue(vm.IsModified);
        
        confirmationService.QueryConfirmationAsync(default!, default!).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync(default!).ReturnsForAnyArgs(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[2].IsSuccess);
        
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("COMMIT");
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("INSERT INTO `tab` (`b`, `a`) VALUES\n(NULL, NULL),\n('txt', 3)");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            "INSERT INTO `tab` (`b`, `a`) VALUES\n(NULL, NULL),\n('txt', 3)",
            "COMMIT",
            "SELECT `b`, `a` FROM `tab`",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_WillNotInsertDeletedRow()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        Assert.AreEqual(3, results.Columns.Count);
        CollectionAssert.AreEqual(new []{"#", "b", "a"}, results.Columns.Select(x => x.Header));

        results.AddRowCommand.Execute();
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("abc");
        results.SelectedCellIndex = 2;
        results.UpdateSelectedCells("2");

        results.AddRowCommand.Execute();
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("txt");
        results.SelectedCellIndex = 2;
        results.UpdateSelectedCells("3");

        results.DeleteRowCommand.Execute();
        
        confirmationService.QueryConfirmationAsync(default, default).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[2].IsSuccess);
        
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("COMMIT");
        await userQuestionsService.Received().ConfirmExecuteQueryAsync("INSERT INTO `tab` (`b`, `a`) VALUES\n('abc', 2)");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            "INSERT INTO `tab` (`b`, `a`) VALUES\n('abc', 2)",
            "COMMIT",
            "SELECT `b`, `a` FROM `tab`",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_NoConfirmCancelsTask()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];
        results.AddRowCommand.Execute();
        
        confirmationService.QueryConfirmationAsync(default, default).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync("START TRANSACTION").Returns(true);
        userQuestionsService.ConfirmExecuteQueryAsync("ROLLBACK").Returns(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.AreEqual(1, results.Count);
        Assert.IsTrue(actionsOutputService.Actions[2].IsFail);
        Assert.IsTrue(actionsOutputService.Actions[2].Response.Contains("cancel", StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(actionsOutputService.Actions[3].IsSuccess);
        Assert.AreEqual("ROLLBACK", actionsOutputService.Actions[3].OriginalQuery);
        
        userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("ROLLBACK");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("INSERT INTO `tab` (`b`, `a`) VALUES\n(NULL, NULL)");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            "ROLLBACK",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_Formats()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "varchar(64)"),
            new ColumnInfo("b", "tinyint(1)"),
            new ColumnInfo("c", "tinyint unsigned"),
            new ColumnInfo("d", "tinyint"),
            new ColumnInfo("e", "smallint"),
            new ColumnInfo("f", "smallint unsigned"),
            new ColumnInfo("g", "int"),
            new ColumnInfo("h", "int unsigned"),
            new ColumnInfo("i", "bigint"),
            new ColumnInfo("j", "bigint unsigned"),
            new ColumnInfo("k", "decimal"),
            new ColumnInfo("l", "double"),
            new ColumnInfo("m", "float"),
            new ColumnInfo("n", "datetime"),
            new ColumnInfo("o", "TIMESTAMP"),
            new ColumnInfo("p", "time"),
            new ColumnInfo("q", "binary(64)"));
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];

        results.AddRowCommand.Execute();
        results.AddRowCommand.Execute();
        results.SelectedCellIndex = 1; results.UpdateSelectedCells("abc");
        results.SelectedCellIndex = 2; results.UpdateSelectedCells("1");
        results.SelectedCellIndex = 3; results.UpdateSelectedCells("255");
        results.SelectedCellIndex = 4; results.UpdateSelectedCells("-127");
        results.SelectedCellIndex = 5; results.UpdateSelectedCells("-32768");
        results.SelectedCellIndex = 6; results.UpdateSelectedCells("65535");
        results.SelectedCellIndex = 7; results.UpdateSelectedCells("-2147483648");
        results.SelectedCellIndex = 8; results.UpdateSelectedCells("4294967295");
        results.SelectedCellIndex = 9; results.UpdateSelectedCells("-9223372036854775808");
        results.SelectedCellIndex = 10; results.UpdateSelectedCells("18446744073709551615");
        results.SelectedCellIndex = 11; results.UpdateSelectedCells("1.1");
        results.SelectedCellIndex = 12; results.UpdateSelectedCells("1.1");
        results.SelectedCellIndex = 13; results.UpdateSelectedCells("1.1");
        results.SelectedCellIndex = 14; results.UpdateSelectedCells("2021-01-01 00:00:00");
        results.SelectedCellIndex = 15; results.UpdateSelectedCells("2021-01-01 00:00:00");
        results.SelectedCellIndex = 16; results.UpdateSelectedCells("20:30:40");
        results.SelectedCellIndex = 17; results.UpdateSelectedCells("DEADBEEF");
        
        confirmationService.QueryConfirmationAsync(default, default).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        await results.ApplyChangesCommand.ExecuteAsync();
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT * FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            @"INSERT INTO `tab` (`a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i`, `j`, `k`, `l`, `m`, `n`, `o`, `p`, `q`) VALUES
(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
('abc', 1, 255, -127, -32768, 65535, -2147483648, 4294967295, -9223372036854775808, 18446744073709551615, 1.1, 1.1, 1.1, '2021-01-01 00:00:00', '2021-01-01 00:00:00', '20:30:40', X'DEADBEEF')".Replace(Environment.NewLine, "\n"),
            "COMMIT",
            "SELECT * FROM `tab`",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_Now_Is_Not_Supported_Yet()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("g", "int", isPrimaryKey: true),
            new ColumnInfo("dt", "datetime"),
            new ColumnInfo("d", "date"),
            new ColumnInfo("t", "time"),
            new ColumnInfo("ts", "TIMESTAMP"));
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];

        results.AddRowCommand.Execute();
        results.SelectedCellIndex = 1; results.UpdateSelectedCells("1");
        results.SelectedCellIndex = 2; results.UpdateSelectedCells("NOW()");
        results.SelectedCellIndex = 3; results.UpdateSelectedCells("NOW()");
        results.SelectedCellIndex = 4; results.UpdateSelectedCells("NOW()");
        results.SelectedCellIndex = 5; results.UpdateSelectedCells("NOW()");
        
        confirmationService.QueryConfirmationAsync(default, default).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        await results.ApplyChangesCommand.ExecuteAsync();
        
        // note: this is wrong, but it's not supported yet
        // so we are testing not-working behavior
        // if this test begins to fail, and the insert is NOW(),
        // then it means that NOW() is supported and this test can be updated
        CollectionAssert.AreEqual(new []
        {
            "SELECT * FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            @"INSERT INTO `tab` (`g`, `dt`, `d`, `t`, `ts`) VALUES
(1, NULL, NULL, NULL, NULL)".Replace(Environment.NewLine, "\n"),
            "COMMIT",
            "SELECT * FROM `tab`",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_VeryLongBinary()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table,new ColumnInfo("a", "binary(64)"));
        byte[] longBytes = Enumerable.Range(0, BinaryColumnData.MaxToStringLength + 10).Select(x => (byte)0xAA).ToArray();
        table.Insert(longBytes);
        var longBytesAsHex = Convert.ToHexString(longBytes);
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];
        results.Selection.Add(0);
        results.CopyInsertCommand.Execute(null);
        clipboard.Received().SetText($@"INSERT INTO `tab` (`a`) VALUES
(X'{longBytesAsHex}')".Replace(Environment.NewLine, "\n"));
        results.DuplicateRowCommand.Execute();

        results.AddRowCommand.Execute();
        results.SelectedCellIndex = 1; results.UpdateSelectedCells(longBytesAsHex);
        
        confirmationService.QueryConfirmationAsync(default, default).ReturnsForAnyArgs(Task.FromResult(QueryConfirmationResult.AlreadyExecuted));
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        await results.ApplyChangesCommand.ExecuteAsync();
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT * FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
            "START TRANSACTION",
            $@"INSERT INTO `tab` (`a`) VALUES
(X'{longBytesAsHex}'),
(X'{longBytesAsHex}')".Replace(Environment.NewLine, "\n"),
            "COMMIT",
            "SELECT * FROM `tab`",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_CopyInsert()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        table.Insert(1, "a");
        table.Insert(2, "b");
        table.Insert(3, "c");
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        Assert.AreEqual(3, results.Columns.Count);
        CollectionAssert.AreEqual(new []{"#", "b", "a"}, results.Columns.Select(x => x.Header));
        
        results.Selection.Add(2);
        results.Selection.Add(1);

        results.CopyInsertCommand.Execute(null);
        
        clipboard.Received().SetText("INSERT INTO `tab` (`b`, `a`) VALUES\n('b', 2),\n('c', 3)");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }

    [Test]
    public async Task Test_SelectTable_Editing()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "varchar", false, true, true),
            new ColumnInfo("b", "int", false, true, true),
            new ColumnInfo("c", "float", false, true, true),
            new ColumnInfo("d", "tinyint", false, true, true),
            new ColumnInfo("e", "text", true, true, true));

        vm.Document.Insert(0, "SHOW TABLES");
        await vm.ExecuteAllCommand.ExecuteAsync();
        

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        var results = vm.Results[0];
        Assert.IsTrue(vm.Results[0] is not SelectSingleTableViewModel);
        Assert.AreEqual(2, results.Columns.Count);
        CollectionAssert.AreEqual(new string[]{"#", "Tables_in_world"}, results.Columns.Select(x => x.Header));
        Assert.AreEqual(1, results.Count);
        Assert.AreEqual(null, results.GetShortValue(0, 0));
        Assert.AreEqual("tab", results.GetShortValue(0, 1));
        
        results.UpdateSelectedCells("abc");
        userQuestionsService.Received().InformCantEditNonSelectAsync();
        
        CollectionAssert.AreEqual(new []
        {
            "SHOW TABLES",
            "SHOW FULL TABLES;"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_CopyPaste_Rows()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "varchar", true, true, true),
            new ColumnInfo("b", "int", true, true, true));
        table.Insert(new object?[]{"a\t\n\"bb\"c", 1});
        table.Insert(new object?[]{"bcd", 2});
        table.Insert(new object?[]{"efg", 3});
        
        vm.Document.Insert(0, "select * from tab");
        await vm.ExecuteAllCommand.ExecuteAsync();
        
        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        var results = vm.Results[0] as SelectSingleTableViewModel;
        Assert.IsNotNull(results);
        
        results!.Selection.Add(0);
        results.Selection.Add(2);
        results.SelectedCellIndex = 1;
        
        results.CopySelectedCommand.Execute(null);
        
        clipboard.Received().SetText("\"a\t\n\\\"bb\\\"c\"\t1\nefg\t3");
        clipboard.GetText().ReturnsForAnyArgs(Task.FromResult<string?>("\"a\t\n\\\"bb\"c\"\t1\nefg\t3"));
        await results.PasteSelectedCommand.ExecuteAsync();
        
        Assert.AreEqual(5, results.Count);
        Assert.AreEqual("a\t\n\"bb\"c", results.GetShortValue(3, 1));
        Assert.AreEqual("1", results.GetShortValue(3, 2));
        Assert.AreEqual("efg", results.GetShortValue(4, 1));
        Assert.AreEqual("3", results.GetShortValue(4, 2));
    }
    
    [Test]
    public async Task Test_InsertAsksForConfirmation()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        table.Insert(new object?[]{1, "abc"});
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        
        results.Selection.Add(0);
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("a");

        preferences.AskBeforeApplyingChanges.ReturnsForAnyArgs(true);
        
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();

        await confirmationService.Received().QueryConfirmationAsync("START TRANSACTION;\nUPDATE `tab` SET `b` = 'a' WHERE `a` = 1;\nCOMMIT", Arg.Any<Func<Task>>());
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SELECT DATABASE()",
            "SELECT * FROM `information_schema`.`COLUMNS` WHERE `TABLE_SCHEMA` = 'world' AND `TABLE_NAME` = 'tab' ORDER BY `ORDINAL_POSITION`",
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_EditViaEditPanel()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "varchar"));
        table.Insert(new object?[]{1, "abc"});
        
        vm.Document.Insert(0, "SELECT `a`, `b` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];

        await results.RefreshTableCommand.ExecuteAsync();
        
        results.SelectedRowIndex = 0;
        results.SelectedCellIndex = 1;

        var editor = (SignedIntegerCellEditorViewModel)results.CellEditor!;
        editor.Value = 3;
        editor.ApplyChanges();
        
        Assert.AreEqual("3", results.GetShortValue(0, 1));
        
        results.SelectedCellIndex = 2;
        var strEditor = (StringCellEditorViewModel)results.CellEditor!;
        strEditor.Document.Text = "def";
        strEditor.ApplyChanges();
        
        Assert.AreEqual("def", results.GetShortValue(0, 2));
    }
    
    [Test]
    public async Task Test_UpdateValueOutOfBounds_WillNotCrash()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "bit(2)", false, true, true));
        table.Insert(new object?[]{1UL});
        
        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];

        await results.RefreshTableCommand.ExecuteAsync();
        
        results.Selection.Add(0);
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("4");

        Assert.Pass();
    }
    
    [Test]
    public async Task Bug_UpdateValueOutOfBounds_WillKeepPreviousValue()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true));
        table.Insert(new object?[]{1});
        
        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        Assert.AreEqual("1", results.GetShortValue(0, 1));
        
        results.Selection.Add(0);
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("a");

        await userQuestionsService.Received().InformEditErrorAsync(Arg.Any<string>());

        Assert.AreEqual("1", results.GetShortValue(0, 1));
        
        Assert.Pass();
    }
    
    [Test]
    public async Task Bug_RefreshWorksAfterTableStructureChange()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true));
        table.Insert(new object?[]{1});
        
        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];

        worldDb.Drop(table);
        
        table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "varchar(255)", false, true, true));
        table.Insert(new object?[]{"abc"});
        
        await results.RefreshTableCommand.ExecuteAsync();
        Assert.AreEqual("abc", results.GetShortValue(0, 1));
        
        results.Selection.Add(0);
        results.SelectedCellIndex = 1;
        results.SelectedRowIndex = 0;
        
        Assert.IsTrue(results.CellEditor is StringCellEditorViewModel);
        ((StringCellEditorViewModel)results.CellEditor!)!.Document.Text = "def";
        results.CellEditor.ApplyChangesCommand.Execute(null);

        Assert.AreEqual("def", results.GetShortValue(0, 1));
        Assert.Pass();
    }

    [Test]
    public async Task Bug_Delayed_Refresh_Will_Reset_State()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true));
        table.Insert(new object?[]{1});

        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);

        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];

        var @lock = mockServer.CreateGlobalLock();

        var refresh = results.RefreshTableCommand.ExecuteAsync();
        results.AddRowCommand.Execute();
        Assert.IsFalse(results.AddRowCommand.CanExecute());

        @lock.Dispose();
        await refresh;

        Assert.AreEqual(1, results.Count);
    }

    [Test]
    public async Task Bug_TimeColumns_PadMicroseconds()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true),
            new ColumnInfo("b", "time(6)"),
            new ColumnInfo("c", "datetime(6)"));
        table.Insert(new object?[]{1, new TimeSpan(0, 0, 0, 0, 0, 1), new MySqlDateTime(2000, 1, 1, 0, 0, 0, 1)});
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];

        Assert.AreEqual("00:00:00.000001", results.GetShortValue(0, 2));
        
        results.Selection.Add(0);
        results.SelectedCellIndex = 2;
        
        results.UpdateSelectedCells("00:00:00.1");
        
        Assert.AreEqual("00:00:00.100000", results.GetShortValue(0, 2));
    }

    [Test]
    public async Task Bug_IncorrectColumnNameAndTypeAfterRefresh()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("a", "int", false, true, true));
        table.Insert(new object?[]{1});
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];

        Assert.AreEqual("a", results.Columns[1].Header);
        worldDb.Drop(table);
        
        table = worldDb.CreateTable("tab", TableType.Table, new ColumnInfo("b", "varchar(255)", false, true, true));
        table.Insert(new object?[]{"abc"});
        
        await results.RefreshTableCommand.ExecuteAsync();
        Assert.AreEqual("b", results.Columns[1].Header);

        results.Selection.Add(0);
        results.SelectedCellIndex = 1;
        results.SelectedRowIndex = 0;
        
        Assert.IsTrue(results.CellEditor is StringCellEditorViewModel);
        Assert.AreEqual("varchar(255)", results.CellEditor!.Type);
    }
    
    [Test]
    public async Task Test_BinaryEditor()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "varbinary(20)", false, true, true));
        table.Insert(new object?[]{new byte[]{}});
        
        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        
        results.SelectedCellIndex = 1;
        
        var editor = (BinaryCellEditorViewModel)results.CellEditor!;
        editor.Length = 5;

        Assert.AreEqual("0000000000", results.GetShortValue(0, 1));
        
        editor.Bytes[0] = 0xAA;
        
        Assert.AreEqual("0000000000", results.GetShortValue(0, 1));
        Assert.IsTrue(results.TryGetRowOverride(0, 1, out var value));
        Assert.AreEqual("0000000000", value);
        
        editor.ApplyChanges();
        
        Assert.AreEqual("AA00000000", results.GetShortValue(0, 1));
    }
    
    [Test]
    public async Task Test_BinaryEditor_SetNull()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, 
            new ColumnInfo("a", "varbinary(20)", false, true, true));
        table.Insert(new object?[]{new byte[]{0x11, 0x22, 0x33, 0x44}});
        
        vm.Document.Insert(0, "SELECT `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        
        results.SelectedCellIndex = 1; 
        
        var editor = (BinaryCellEditorViewModel)results.CellEditor!;
        editor.Bytes[0] = 0x55;
        editor.ApplyChanges();

        Assert.AreEqual("55223344", results.GetShortValue(0, 1));
        
        results.Selection.Add(0);
        results.SetSelectedToNullCommand.Execute();
        
        Assert.IsNull(results.TableController.GetCellText(0, 1));
        Assert.IsTrue(results.TryGetRowOverride(0, 1, out var overrideString));
        Assert.IsNull(overrideString);
    }
}