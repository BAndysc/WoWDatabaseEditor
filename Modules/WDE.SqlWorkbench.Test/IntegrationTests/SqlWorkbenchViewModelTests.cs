using System.Diagnostics.CodeAnalysis;
using MySqlConnector;
using NSubstitute;
using WDE.Common.Services;
using WDE.Common.Tasks;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Services.ActionsOutput;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.LanguageServer;
using WDE.SqlWorkbench.Services.QueryUtils;
using WDE.SqlWorkbench.Services.UserQuestions;
using WDE.SqlWorkbench.Settings;
using WDE.SqlWorkbench.Test.Mock;
using WDE.SqlWorkbench.ViewModels;

namespace WDE.SqlWorkbench.Test.IntegrationTests;

[SuppressMessage("Assertion", "NUnit2005:Consider using Assert.That(actual, Is.EqualTo(expected)) instead of Assert.AreEqual(expected, actual)")]
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
    protected MockSqlConnector.MockMemoryServer mockServer = null!;
    protected ManualSynchronizationContext synchronizationContext = null!;
    
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

    protected SqlWorkbenchViewModel CreateViewModel(DatabaseConnectionData connectionData)
    {
        var connection = new Connection(connector, connectionData);
        var vm = new SqlWorkbenchViewModel(actionsOutputService, languageServer, configuration, queryUtility, userQuestionsService, preferences, clipboard, mainThread, connection);
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
        userQuestionsService.Received().ConnectionsErrorAsync(Arg.Any<MySqlException>());
    }
    
    [Test]
    public async Task Test_ShowTables_CantEdit()
    {
        using var vm = CreateConnectedViewModel();
        vm.Document.Insert(0, "SHOW TABLES");
        await vm.ExecuteAllCommand.ExecuteAsync();
        
        Assert.AreEqual("Error: Unknown database 'world'", actionsOutputService.Actions[0].Response);
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new[]{typeof(string)}, new ColumnInfo("a", "varchar", false, true, true, null));

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
        userQuestionsService.Received().InformCantEditNonSelectAsync();
        
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
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(int), typeof(string)}, new ColumnInfo("a", "int", false, true, true, null),
            new ColumnInfo("b", "varchar", false, false, false, null));
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
        userQuestionsService.Received().NoFullPrimaryKeyAsync();
        Assert.AreEqual("text", results.GetShortValue(0, 1));
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b` FROM `tab`",
            "SHOW FULL TABLES;",
            "SHOW COLUMNS FROM `tab`"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_CanEditSelectWithPrimaryKey()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(int), typeof(string)}, new ColumnInfo("a", "int", false, true, true, null),
            new ColumnInfo("b", "varchar", false, false, false, null));
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
        
        userQuestionsService.ConfirmExecuteQueryAsync("START TRANSACTION").Returns(true);
        userQuestionsService.ConfirmExecuteQueryAsync("COMMIT").Returns(true);
        userQuestionsService.ConfirmExecuteQueryAsync("UPDATE `tab` SET `a` = 3 WHERE `a` = 5").Returns(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[2].IsSuccess);
        
        userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("COMMIT");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("UPDATE `tab` SET `a` = 3 WHERE `a` = 5");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SHOW COLUMNS FROM `tab`",
            "START TRANSACTION",
            "UPDATE `tab` SET `a` = 3 WHERE `a` = 5",
            "COMMIT"
        }, connector.ExecutedQueries);
    }

    [Test]
    public async Task Test_CanInsertRows()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(int), typeof(string)}, new ColumnInfo("a", "int", false, true, true, null),
            new ColumnInfo("b", "varchar", false, false, false, null));
        
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
        
        results.AddRowCommand.Execute(null);
        results.AddRowCommand.Execute(null);
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("txt");
        results.SelectedCellIndex = 2;
        results.UpdateSelectedCells("3");

        Assert.IsTrue(results.IsModified);
        Assert.IsTrue(vm.IsModified);
        
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[2].IsSuccess);
        
        userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("COMMIT");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("INSERT INTO `tab` (`b`, `a`) VALUES\n(NULL, NULL),\n('txt', 3)");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SHOW COLUMNS FROM `tab`",
            "START TRANSACTION",
            "INSERT INTO `tab` (`b`, `a`) VALUES\n(NULL, NULL),\n('txt', 3)",
            "COMMIT"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_WillNotInsertDeletedRow()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(int), typeof(string)}, new ColumnInfo("a", "int", false, true, true, null),
            new ColumnInfo("b", "varchar", false, false, false, null));
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        Assert.IsTrue(actionsOutputService.Actions[0].IsSuccess);
        Assert.AreEqual(1, vm.Results.Count);
        
        // results
        Assert.IsTrue(vm.Results[0] is SelectSingleTableViewModel);
        var results = (SelectSingleTableViewModel)vm.Results[0];
        Assert.AreEqual(3, results.Columns.Count);
        CollectionAssert.AreEqual(new []{"#", "b", "a"}, results.Columns.Select(x => x.Header));

        results.AddRowCommand.Execute(null);
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("abc");
        results.SelectedCellIndex = 2;
        results.UpdateSelectedCells("2");

        results.AddRowCommand.Execute(null);
        results.SelectedCellIndex = 1;
        results.UpdateSelectedCells("txt");
        results.SelectedCellIndex = 2;
        results.UpdateSelectedCells("3");

        results.DeleteRowCommand.Execute(null);
        
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
        Assert.IsTrue(actionsOutputService.Actions[2].IsSuccess);
        
        userQuestionsService.Received().ConfirmExecuteQueryAsync("START TRANSACTION");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("COMMIT");
        userQuestionsService.Received().ConfirmExecuteQueryAsync("INSERT INTO `tab` (`b`, `a`) VALUES\n('abc', 2)");
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT `b`, `a` FROM `tab`",
            "SHOW FULL TABLES;",
            "SHOW COLUMNS FROM `tab`",
            "START TRANSACTION",
            "INSERT INTO `tab` (`b`, `a`) VALUES\n('abc', 2)",
            "COMMIT"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_NoConfirmCancelsTask()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(int), typeof(string)}, new ColumnInfo("a", "int", false, true, true, null),
            new ColumnInfo("b", "varchar", false, false, false, null));
        
        vm.Document.Insert(0, "SELECT `b`, `a` FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];
        results.AddRowCommand.Execute(null);
        
        userQuestionsService.ConfirmExecuteQueryAsync("START TRANSACTION").Returns(true);
        userQuestionsService.ConfirmExecuteQueryAsync("ROLLBACK").Returns(true);
        Assert.IsTrue(results.ApplyChangesCommand.CanExecute(null));
        await results.ApplyChangesCommand.ExecuteAsync();
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
            "SHOW COLUMNS FROM `tab`",
            "START TRANSACTION",
            "ROLLBACK"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_Formats()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []
            {
                typeof(string),
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(decimal),
                typeof(double),
                typeof(float),
                typeof(MySqlDateTime),
                typeof(DateTime),
                typeof(TimeSpan),
                typeof(byte[])
            }, 
            new ColumnInfo("a", "varchar(64)", false, false, false, null),
            new ColumnInfo("b", "tinyint(1)", false, false, false, null),
            new ColumnInfo("c", "tinyint unsigned", false, false, false, null),
            new ColumnInfo("d", "tinyint", false, false, false, null),
            new ColumnInfo("e", "smallint", false, false, false, null),
            new ColumnInfo("f", "smallint unsigned", false, false, false, null),
            new ColumnInfo("g", "int", false, false, false, null),
            new ColumnInfo("h", "int unsigned", false, false, false, null),
            new ColumnInfo("i", "bingint", false, false, false, null),
            new ColumnInfo("j", "bigint unsigned", false, false, false, null),
            new ColumnInfo("k", "decimal", false, false, false, null),
            new ColumnInfo("l", "double", false, false, false, null),
            new ColumnInfo("m", "float", false, false, false, null),
            new ColumnInfo("n", "datetime", false, false, false, null),
            new ColumnInfo("o", "TIMESTAMP", false, false, false, null),
            new ColumnInfo("p", "time", false, false, false, null),
            new ColumnInfo("q", "binary(64)", false, false, false, null));
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];

        results.AddRowCommand.Execute(null);
        results.AddRowCommand.Execute(null);
        results.SelectedCellIndex = 1; results.UpdateSelectedCells("abc");
        results.SelectedCellIndex = 2; results.UpdateSelectedCells("true");
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
        results.SelectedCellIndex = 15; results.UpdateSelectedCells("NOW()");
        results.SelectedCellIndex = 16; results.UpdateSelectedCells("20:30:40");
        results.SelectedCellIndex = 17; results.UpdateSelectedCells("DEADBEEF");
        
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        await results.ApplyChangesCommand.ExecuteAsync();
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT * FROM `tab`",
            "SHOW FULL TABLES;",
            "SHOW COLUMNS FROM `tab`",
            "START TRANSACTION",
            @"INSERT INTO `tab` (`a`, `b`, `c`, `d`, `e`, `f`, `g`, `h`, `i`, `j`, `k`, `l`, `m`, `n`, `o`, `p`, `q`) VALUES
(NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
('abc', true, 255, -127, -32768, 65535, -2147483648, 4294967295, -9223372036854775808, 18446744073709551615, 1.1, 1.1, 1.1, '2021-01-01 00:00:00', NOW(), '20:30:40', X'DEADBEEF')",
            "COMMIT"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_Insert_VeryLongBinary()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []
            {
                typeof(byte[])
            }, 
            new ColumnInfo("a", "binary(64)", false, false, false, null));
        byte[] longBytes = Enumerable.Range(0, BinaryColumnData.MaxToStringLength + 10).Select(x => (byte)0xAA).ToArray();
        table.Insert(longBytes);
        var longBytesAsHex = Convert.ToHexString(longBytes);
        
        vm.Document.Insert(0, "SELECT * FROM `tab`");
        await vm.ExecuteAllCommand.ExecuteAsync();

        var results = (SelectSingleTableViewModel)vm.Results[0];
        results.Selection.Add(0);
        results.CopyInsertCommand.Execute(null);
        clipboard.Received().SetText($@"INSERT INTO `tab` (`a`) VALUES
(X'{longBytesAsHex}')");
        results.DuplicateRowCommand.Execute(null);

        results.AddRowCommand.Execute(null);
        results.SelectedCellIndex = 1; results.UpdateSelectedCells(longBytesAsHex);
        
        userQuestionsService.ConfirmExecuteQueryAsync(default).ReturnsForAnyArgs(true);
        await results.ApplyChangesCommand.ExecuteAsync();
        
        CollectionAssert.AreEqual(new []
        {
            "SELECT * FROM `tab`",
            "SHOW FULL TABLES;",
            "SHOW COLUMNS FROM `tab`",
            "START TRANSACTION",
            $@"INSERT INTO `tab` (`a`) VALUES
(X'{longBytesAsHex}'),
(X'{longBytesAsHex}')",
            "COMMIT"
        }, connector.ExecutedQueries);
    }
    
    [Test]
    public async Task Test_CopyInsert()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(int), typeof(string)}, new ColumnInfo("a", "int", false, true, true, null),
            new ColumnInfo("b", "varchar", false, false, false, null));
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
            "SHOW COLUMNS FROM `tab`"
        }, connector.ExecutedQueries);
    }

    [Test]
    public async Task Test_SelectTable_Editing()
    {
        using var vm = CreateConnectedViewModel();
        var worldDb = mockServer.CreateDatabase("world");
        var table = worldDb.CreateTable("tab", TableType.Table, new []{typeof(string), typeof(int), typeof(float), typeof(sbyte), typeof(string)}, new ColumnInfo("a", "varchar", false, true, true, null),
            new ColumnInfo("b", "int", false, true, true, null),
            new ColumnInfo("c", "float", false, true, true, null),
            new ColumnInfo("d", "tinyint", false, true, true, null),
            new ColumnInfo("e", "text", true, true, true, null));

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
}