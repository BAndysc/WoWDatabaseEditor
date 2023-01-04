using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Antlr4.Runtime;
using MySqlConnector;
using WDE.Common.Disposables;
using WDE.Common.Utils;
using WDE.SqlWorkbench.Antlr;
using WDE.SqlWorkbench.Models;
using WDE.SqlWorkbench.Models.DataTypes;
using WDE.SqlWorkbench.Services.Connection;
using WDE.SqlWorkbench.Services.QueryUtils;

namespace WDE.SqlWorkbench.Test.Mock;

internal static class MySqlExceptions
{
    public static MySqlException Create(MySqlErrorCode errorCode, string message = "")
    {
        var ctor = typeof(MySqlException).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
            .FirstOrDefault(c => c.GetParameters().Length == 2 && c.GetParameters()[0].ParameterType == typeof(MySqlErrorCode) && c.GetParameters()[1].ParameterType == typeof(string))!;
        return (MySqlException)ctor.Invoke(new object[2]{errorCode, message});
    }
}

[ExcludeFromCodeCoverage]
internal class MockSqlConnector : IMySqlConnector
{
    private readonly IQuerySafetyService querySafetyService;
    private Dictionary<string, MockMemoryServer> servers = new();
    private List<string> executedQueries = new();
            
    public IReadOnlyList<string> ExecutedQueries => executedQueries;

    public MockSqlConnector(IQuerySafetyService querySafetyService)
    {
        this.querySafetyService = querySafetyService;
    }
    
    public async Task<IRawMySqlConnection> ConnectAsync(DatabaseCredentials credentials, QueryExecutionSafety safeMode)
    {
        if (!servers.TryGetValue($"{credentials.Host}:{credentials.Port}", out var server))
            throw MySqlExceptions.Create(MySqlErrorCode.UnableToConnectToHost, "Can't find server");
        
        if (!server.HasUser(credentials.User, credentials.Passwd))
            throw MySqlExceptions.Create(MySqlErrorCode.PasswordNoMatch, "Can't find user");

        return server.Connect(credentials.SchemaName, safeMode);
    }
    
    public MockMemoryServer CreateServer(string host, string user, string password, int port)
    {
        var server = new MockMemoryServer(this, querySafetyService, host, port);
        server.AddUser(user, password);
        servers.Add($"{host}:{port}", server);
        return server;
    }
    
    internal class MockMemoryServer
    {
        public string Host { get; }
        public int Port { get; }
        private readonly MockSqlConnector mockSqlConnector;
        private readonly IQuerySafetyService safetyService;
        private readonly Dictionary<string, MockMemoryDatabase> databases = new();
        private readonly HashSet<(string user, string pass)> users = new();
        public readonly MockMemoryDatabase InformationSchema;
        public readonly MockMemoryDatabase.Table TablesTable;
        public readonly MockMemoryDatabase.Table SchemataTable;
        public readonly MockMemoryDatabase.Table ColumnsTable;
        public readonly MockMemoryDatabase.Table RoutinesTable;
        public readonly MockMemoryDatabase.Table EnginesTable;
        public readonly MockMemoryDatabase.Table CollationsTable;

        private TaskCompletionSource? globalLock;

        /// <summary>
        /// to simulate long pending tasks, this can create an infinite delay in commands execution until released
        /// </summary>
        /// <returns></returns>
        public System.IDisposable CreateGlobalLock()
        {
            globalLock = new TaskCompletionSource();
            return new ActionDisposable(() =>
            {
                globalLock.SetResult();
                globalLock = null;
            });
        }

        public MockMemoryServer(
            MockSqlConnector mockSqlConnector,
            IQuerySafetyService safetyService, 
            string host, 
            int port)
        {
            Host = host;
            Port = port;
            this.mockSqlConnector = mockSqlConnector;
            this.safetyService = safetyService;
            InformationSchema = CreateDatabaseInternal("information_schema");
            ColumnsTable = InformationSchema.CreateTableInternal("COLUMNS",
                TableType.View,
                new ColumnInfo("TABLE_CATALOG", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("TABLE_SCHEMA", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("TABLE_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("COLUMN_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("ORDINAL_POSITION", "int unsigned", false, false, false, null, null, null),
                new ColumnInfo("COLUMN_DEFAULT", "text", true, false, false, null, null, null),
                new ColumnInfo("IS_NULLABLE", "varchar(3)", false, false, false, null, null, null),
                new ColumnInfo("DATA_TYPE", "longtext", true, false, false, null, null, null),
                new ColumnInfo("CHARACTER_MAXIMUM_LENGTH", "bigint", true, false, false, null, null, null),
                new ColumnInfo("CHARACTER_OCTET_LENGTH", "bigint", true, false, false, null, null, null),
                new ColumnInfo("NUMERIC_PRECISION", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("NUMERIC_SCALE", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("DATETIME_PRECISION", "int unsigned", true, false, false, null, null, null),
                new ColumnInfo("CHARACTER_SET_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("COLLATION_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("COLUMN_TYPE", "mediumtext", false, false, false, null, null, null),
                new ColumnInfo("COLUMN_KEY", "enum('','PRI','UNI','MUL')", false, false, false, null, null, null),
                new ColumnInfo("EXTRA", "varchar(256)", true, false, false, null, null, null),
                new ColumnInfo("PRIVILEGES", "varchar(154)", true, false, false, null, null, null),
                new ColumnInfo("COLUMN_COMMENT", "text", false, false, false, null, null, null),
                new ColumnInfo("GENERATION_EXPRESSION", "longtext", false, false, false, null, null, null),
                new ColumnInfo("SRS_ID", "int unsigned", true, false, false, null, null, null));
            
            TablesTable = InformationSchema.CreateTableInternal("TABLES",
                TableType.View,
                new ColumnInfo("TABLE_CATALOG", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("TABLE_SCHEMA", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("TABLE_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("TABLE_TYPE", "enum('BASE TABLE','VIEW','SYSTEM VIEW')", false, false, false, null, null, null),
                new ColumnInfo("ENGINE", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("VERSION", "int", true, false, false, null, null, null),
                new ColumnInfo("ROW_FORMAT", "enum('Fixed','Dynamic','Compressed','Redundant','Compact','Paged')", true, false, false, null, null, null),
                new ColumnInfo("TABLE_ROWS", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("AVG_ROW_LENGTH", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("DATA_LENGTH", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("MAX_DATA_LENGTH", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("INDEX_LENGTH", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("DATA_FREE", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("AUTO_INCREMENT", "bigint unsigned", true, false, false, null, null, null),
                new ColumnInfo("CREATE_TIME", "timestamp", false, false, false, null, null, null),
                new ColumnInfo("UPDATE_TIME", "datetime", true, false, false, null, null, null),
                new ColumnInfo("CHECK_TIME", "datetime", true, false, false, null, null, null),
                new ColumnInfo("TABLE_COLLATION", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("CHECKSUM", "bigint", true, false, false, null, null, null),
                new ColumnInfo("CREATE_OPTIONS", "varchar(256)", true, false, false, null, null, null),
                new ColumnInfo("TABLE_COMMENT", "text", true, false, false, null, null, null));

            SchemataTable = InformationSchema.CreateTableInternal("SCHEMATA",
                TableType.View,
                new ColumnInfo("CATALOG_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("SCHEMA_NAME", "varchar(64)", true, false, false, null, null, null),
                new ColumnInfo("DEFAULT_CHARACTER_SET_NAME", "varchar(64)", false, false, false, null, null, null),
                new ColumnInfo("DEFAULT_COLLATION_NAME", "varchar(64)", false, false, false, null, null, null),
                new ColumnInfo("SQL_PATH", "binary(0)", true, false, false, null, null, null),
                new ColumnInfo("DEFAULT_ENCRYPTION", "enum('NO','YES')", false, false, false, null, null, null));

            RoutinesTable = InformationSchema.CreateTable("ROUTINES",
                TableType.View,
                new ColumnInfo("SPECIFIC_NAME", "varchar", false, false, false, null, null, null),
                new ColumnInfo("ROUTINE_CATALOG", "varchar", true, false, false, null, null, null),
                new ColumnInfo("ROUTINE_SCHEMA", "varchar", true, false, false, null, null, null),
                new ColumnInfo("ROUTINE_NAME", "varchar", false, false, false, null, null, null),
                new ColumnInfo("ROUTINE_TYPE", "enum", false, false, false, null, null, null),
                new ColumnInfo("DATA_TYPE", "longtext", true, false, false, null, null, null),
                new ColumnInfo("CHARACTER_MAXIMUM_LENGTH", "bigint", true, false, false, null, null, null),
                new ColumnInfo("CHARACTER_OCTET_LENGTH", "bigint", true, false, false, null, null, null),
                new ColumnInfo("NUMERIC_PRECISION", "int", true, false, false, null, null, null),
                new ColumnInfo("NUMERIC_SCALE", "int", true, false, false, null, null, null),
                new ColumnInfo("DATETIME_PRECISION", "int", true, false, false, null, null, null),
                new ColumnInfo("CHARACTER_SET_NAME", "varchar", true, false, false, null, null, null),
                new ColumnInfo("COLLATION_NAME", "varchar", true, false, false, null, null, null),
                new ColumnInfo("DTD_IDENTIFIER", "longtext", true, false, false, null, null, null),
                new ColumnInfo("ROUTINE_BODY", "varchar", false, false, false, "", null, null),
                new ColumnInfo("ROUTINE_DEFINITION", "longtext", true, false, false, null, null, null),
                new ColumnInfo("EXTERNAL_NAME", "binary", true, false, false, null, null, null),
                new ColumnInfo("EXTERNAL_LANGUAGE", "varchar", false, false, false, "SQL", null, null),
                new ColumnInfo("PARAMETER_STYLE", "varchar", false, false, false, "", null, null),
                new ColumnInfo("IS_DETERMINISTIC", "varchar", false, false, false, "", null, null),
                new ColumnInfo("SQL_DATA_ACCESS", "enum", false, false, false, null, null, null),
                new ColumnInfo("SQL_PATH", "binary", true, false, false, null, null, null),
                new ColumnInfo("SECURITY_TYPE", "enum", false, false, false, null, null, null),
                new ColumnInfo("CREATED", "timestamp", false, false, false, null, null, null),
                new ColumnInfo("LAST_ALTERED", "timestamp", false, false, false, null, null, null),
                new ColumnInfo("SQL_MODE", "set", false, false, false, null, null, null),
                new ColumnInfo("ROUTINE_COMMENT", "text", false, false, false, null, null, null),
                new ColumnInfo("DEFINER", "varchar", false, false, false, null, null, null),
                new ColumnInfo("CHARACTER_SET_CLIENT", "varchar", false, false, false, null, null, null),
                new ColumnInfo("COLLATION_CONNECTION", "varchar", false, false, false, null, null, null),
                new ColumnInfo("DATABASE_COLLATION", "varchar", false, false, false, null, null, null));
            
            EnginesTable = InformationSchema.CreateTable("ENGINES",
                TableType.View,
                new ColumnInfo("ENGINE", "varchar(64)", false, false, false, null, null, null),
                new ColumnInfo("SUPPORT", "varchar(8)", false, false, false, null, null, null),
                new ColumnInfo("COMMENT", "varchar(80)", false, false, false, null, null, null),
                new ColumnInfo("TRANSACTIONS", "varchar(3)", true, false, false, null, null, null),
                new ColumnInfo("XA", "varchar(3)", true, false, false, null, null, null),
                new ColumnInfo("SAVEPOINTS", "varchar(3)", true, false, false, null, null, null));
            EnginesTable.Insert(new object?[]{"MEMORY", "YES", "Hash based, stored in memory, useful for temporary tables", "NO", "NO", "NO"});
            
            CollationsTable = InformationSchema.CreateTable("COLLATIONS", TableType.View,
                new ColumnInfo("COLLATION_NAME", "varchar(64)", false, false, false, null, null, null),
                new ColumnInfo("CHARACTER_SET_NAME", "varchar(64)", false, false, false, null, null, null),
                new ColumnInfo("ID", "bigint", false, false, false, null, null, null),
                new ColumnInfo("IS_DEFAULT", "varchar(3)", false, false, false, null, null, null),
                new ColumnInfo("IS_COMPILED", "varchar(3)", false, false, false, null, null, null),
                new ColumnInfo("SORTLEN", "int unsigned", false, false, false, null, null, null),
                new ColumnInfo("PAD_ATTRIBUTE", "enum('PAD SPACE','NO PAD')", false, false, false, null, null, null));
            
            CollationsTable.Insert(new object?[]{"utf8mb4_general_ci", "utf8mb4", 45L, "", "Yes", 1, "PAD SPACE"});
            
            InformationSchema.FinalizeCreateTableInternal(TablesTable);
            InformationSchema.FinalizeCreateTableInternal(SchemataTable);
            InformationSchema.FinalizeCreateTableInternal(ColumnsTable);
            FinalizeCreateDatabase(InformationSchema);
        }

        public void Kill()
        {
            mockSqlConnector.RemoveServer(this);
        }

        public void AddUser(string user, string password)
        {
            users.Add((user, password));
        }
        
        public MockMemoryDatabase CreateDatabase(string databaseName)
        {
            var db = CreateDatabaseInternal(databaseName);
            FinalizeCreateDatabase(db);
            return db;
        }
        
        private MockMemoryDatabase CreateDatabaseInternal(string databaseName)
        {
            var db = databases[databaseName] = new MockMemoryDatabase(this, databaseName);
            return db;
        }

        private void FinalizeCreateDatabase(MockMemoryDatabase database)
        {
            SchemataTable.Insert(new object?[]
            {
                "def",
                database.Name,
                "utf8mb4",
                "utf8mb4_general_ci",
                null,
                "NO"
            });
        }
        
        public bool HasUser(string user, string password)
        {
            return users.Contains((user, password));
        }

        public IRawMySqlConnection Connect(string? currentDatabase, QueryExecutionSafety safeMode)
        {
            return new MockConnection(mockSqlConnector, this, safetyService, currentDatabase, safeMode);
        }

        internal class MockConnection : IRawMySqlConnection
        {
            private readonly MockSqlConnector mockSqlConnector;
            private readonly MockMemoryServer server;
            private readonly IQuerySafetyService querySafetyService;
            private readonly string? currentDatabase;
            private readonly QueryExecutionSafety queryExecutionSafety;

            public MockConnection(MockSqlConnector mockSqlConnector,
                MockMemoryServer server,
                IQuerySafetyService querySafetyService,
                string? currentDatabase,
                QueryExecutionSafety queryExecutionSafety)
            {
                this.mockSqlConnector = mockSqlConnector;
                this.server = server;
                this.querySafetyService = querySafetyService;
                this.currentDatabase = currentDatabase;
                this.queryExecutionSafety = queryExecutionSafety;
            }

            public void Dispose()
            {
                IsSessionOpened = false;
            }

            public async ValueTask DisposeAsync()
            {
                IsSessionOpened = false;
            }

            private class MockDataReader : IMySqlDataReader
            {
                private readonly string? databaseName1;

                public MockDataReader(string? databaseName)
                {
                    databaseName1 = databaseName;
                }

                public bool IsDBNull(int ordinal) => databaseName1 == null;
                public object? GetValue(int ordinal) => databaseName1;
                public string? GetString(int ordinal) => databaseName1;
                public bool GetBoolean(int ordinal) => throw new NotImplementedException();
                public byte GetByte(int ordinal) => throw new NotImplementedException();
                public sbyte GetSByte(int ordinal) => throw new NotImplementedException();
                public short GetInt16(int ordinal) => throw new NotImplementedException();
                public ushort GetUInt16(int ordinal) => throw new NotImplementedException();
                public int GetInt32(int ordinal) => throw new NotImplementedException();
                public uint GetUInt32(int ordinal) => throw new NotImplementedException();
                public long GetInt64(int ordinal) => throw new NotImplementedException();
                public ulong GetUInt64(int ordinal) => throw new NotImplementedException();
                public char GetChar(int ordinal) => throw new NotImplementedException();
                public PublicMySqlDecimal GetDecimal(int ordinal) => throw new NotImplementedException();
                public double GetDouble(int ordinal) => throw new NotImplementedException();
                public float GetFloat(int ordinal) => throw new NotImplementedException();
                public DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
                public TimeSpan GetTimeSpan(int ordinal) => throw new NotImplementedException();
                public long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotImplementedException();
                public MySqlDateTime GetMySqlDateTime(int ordinal) => throw new NotImplementedException();
            }

            public async Task<SelectResult> ExecuteSqlAsync(string queryString, int? rowsLimit = null, CancellationToken token = default)
            {
                if (server.globalLock != null)
                    await server.globalLock.Task;

                if (!await querySafetyService.CanExecuteAsync(queryString, queryExecutionSafety))
                    throw new TaskCanceledException("The user canceled the query execution");
                
                if (!IsSessionOpened)
                    throw new ObjectDisposedException("Session is disposed");
                
                mockSqlConnector.executedQueries.Add(queryString);
                
                var inputStream = new NoCopyStringCharStream(queryString);
                var lexer = new MySQLLexer(inputStream);
                var tokens = new CommonTokenStream(lexer);
                var parser = new MySQLParser(tokens);
                var listener = new MySQLParserBaseListener();
                lexer.RemoveErrorListeners();
                parser.RemoveErrorListeners();

                var query = parser.query();
                var simpleStatement = query.GetChild(0) as MySQLParser.SimpleStatementContext;

                var showTablesRegex = new Regex(@"SHOW\s+TABLES", RegexOptions.IgnoreCase);
                var showFullTablesRegex = new Regex(@"SHOW\s+FULL\s+TABLES", RegexOptions.IgnoreCase);
                var showDatabasesRegex = new Regex(@"SHOW\s+DATABASES", RegexOptions.IgnoreCase);
                var showColumnsRegex = new Regex(@"SHOW\s+COLUMNS\s+FROM\s+`?(.*?)`?$", RegexOptions.IgnoreCase);
                var showIndex = new Regex(@"SHOW\s+INDEX\s+FROM\s+`?(.*?)`?$", RegexOptions.IgnoreCase);
                var beginTransactionRegex = new Regex(@"(BEGIN|START TRANSACTION)", RegexOptions.IgnoreCase);
                var commitTransactionRegex = new Regex(@"COMMIT", RegexOptions.IgnoreCase);
                var rollbackTransactionRegex = new Regex(@"ROLLBACK", RegexOptions.IgnoreCase);

                var showTablesMatch = showTablesRegex.Match(queryString);
                var showFullTablesMatch = showFullTablesRegex.Match(queryString);
                var showDatabasesMatch = showDatabasesRegex.Match(queryString);
                var showColumnsMatch = showColumnsRegex.Match(queryString);
                var showIndexMatch = showIndex.Match(queryString);
                var beginTransactionMatch = beginTransactionRegex.Match(queryString);
                var commitTransactionMatch = commitTransactionRegex.Match(queryString);
                var rollbackTransactionMatch = rollbackTransactionRegex.Match(queryString);

                if (string.Equals(queryString, "SELECT DATABASE()", StringComparison.OrdinalIgnoreCase))
                {
                    var column = new StringColumnData();
                    column.Append(new MockDataReader(currentDatabase), 0);
                    return SelectResult.Query(new[]{"database()"}, new[]{typeof(string)}, 1, new IReadOnlyColumnData[1]{column});
                }
                else if (showTablesMatch.Success)
                {
                    var database = server.GetDatabase(currentDatabase);
                    return server.TablesTable.SelectColumns(new[] { "TABLE_NAME AS " + "Tables_in_" + currentDatabase }, row => (string?)row["TABLE_SCHEMA"] == currentDatabase);
                }
                else if (showFullTablesMatch.Success)
                {
                    var database = server.GetDatabase(currentDatabase);
                    return server.TablesTable.SelectColumns(new[] { "TABLE_NAME AS " + "Tables_in_" + currentDatabase, "TABLE_TYPE" }, row => (string?)row["TABLE_SCHEMA"] == currentDatabase);
                }
                else if (showDatabasesMatch.Success)
                {
                    return server.SchemataTable.SelectColumns(new[] { "SCHEMA_NAME AS DATABASE" }, null);
                }
                else if (showColumnsMatch.Success)
                {
                    return server.ColumnsTable.SelectColumns(new[] { "COLUMN_NAME AS Field", "COLUMN_TYPE AS Type", "IS_NULLABLE AS Null", "COLUMN_KEY AS Key", "COLUMN_DEFAULT AS Default", "EXTRA AS Extra" }, 
                        row => (string?)row["TABLE_SCHEMA"] == currentDatabase && (string?)row["TABLE_NAME"] == showColumnsMatch.Groups[1].Value);
                }
                else if (showIndexMatch.Success)
                {
                    var columns = new IReadOnlyColumnData[]
                    {
                        new StringColumnData(),
                        new Int64ColumnData(),
                        new StringColumnData(),
                        new Int64ColumnData(),
                        new StringColumnData(),
                        new StringColumnData(),
                        new Int64ColumnData(),
                        new ObjectColumnData(),
                        new StringColumnData(),
                        new StringColumnData(),
                        new StringColumnData(),
                        new StringColumnData(),
                        new StringColumnData(),
                        new StringColumnData()
                    };
                    return SelectResult.Query(new[]
                    {
                        "Table",
                        "Non_unique",
                        "Key_name",
                        "Seq_in_index",
                        "Column_name",
                        "Collation",
                        "Sub_part",
                        "Packed",
                        "Null",
                        "Index_type",
                        "Comment",
                        "Index_comment",
                        "Visible",
                        "Expression"
                    }, 
                    new[]
                    {
                        typeof(string),
                        typeof(long),
                        typeof(string),
                        typeof(long),
                        typeof(string),
                        typeof(string),
                        typeof(long),
                        typeof(object),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(string),
                        typeof(string)
                    }, 0, columns);
                }
                else if (beginTransactionMatch.Success)
                {
                    return SelectResult.NonQuery(0);
                }
                else if (commitTransactionMatch.Success)
                {
                    return SelectResult.NonQuery(0);
                }
                else if (rollbackTransactionMatch.Success)
                {
                    return SelectResult.NonQuery(0);
                }
                
                var selectRegex = new Regex(@"SELECT\s*(.*?)\s* FROM\s*([^ ]*?)\s*(?:WHERE\s+(.*?))?(?:ORDER BY\s+(.*?))?;?$", RegexOptions.IgnoreCase);
                var selectMatch = selectRegex.Match(queryString);

                string RemoveBackTicks(string s) => s.Replace("`", "");

                if (selectMatch.Success)
                {
                    var columns = selectMatch.Groups[1].Value.Split(',').Select(x => x.Trim()).Select(RemoveBackTicks).ToArray();
                    var from = RemoveBackTicks(selectMatch.Groups[2].Value.Trim());
                    var fromDatabase = currentDatabase;

                    if (from.Contains("."))
                    {
                        fromDatabase = from.Split('.')[0];
                        from = from.Split('.')[1];
                    }

                    Func<Dictionary<string, object?>, bool>? predicate = null;
                    
                    var where = selectMatch.Groups[3].Value.Trim();
                    if (selectMatch.Groups[3].Success)
                    {
                        var conditions = where.Split("AND").Select(x => x.Trim()).ToArray();
                        foreach (var cond in conditions)
                        {
                            var whereColumn = RemoveBackTicks(cond.Split("=")[0].Trim());
                            var whereValue = cond.Split("=")[1].Trim().Replace("'", "");
                            Func<Dictionary<string, object?>, bool> newPred = row => row[whereColumn]!.Equals(whereValue);
                            if (predicate == null)
                                predicate = newPred;
                            else
                            {
                                var p = predicate;
                                predicate = row => p(row) && newPred(row);
                            }
                        }
                    }
                    
                    var database = server.GetDatabase(fromDatabase);
                    var table = database.GetTable(from);

                    return table.SelectColumns(columns, predicate);
                }

                if (queryString.StartsWith("insert", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("INSERT ignored in mock database, but mocked as a successful insert");
                    return SelectResult.NonQuery(1);
                }
                
                if (queryString.StartsWith("update", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("UPDATE ignored in mock database, but mocked as a successful insert");
                    return SelectResult.NonQuery(1);
                }

                throw new NotImplementedException();
            }

            public bool IsSessionOpened { get; private set; } = true;
        }

        private MockMemoryDatabase GetDatabase(string? database)
        {
            if (database == null)
                throw MySqlExceptions.Create(MySqlErrorCode.NoDatabaseSelected);

            if (!databases.TryGetValue(database, out var db))
                throw MySqlExceptions.Create(MySqlErrorCode.UnknownDatabase, $"Unknown database '{database}'");

            return db;
        }

        private IReadOnlyList<string> GetDatabases()
        {
            return databases.Select(d => d.Key).ToList();
        }
    }

    private void RemoveServer(MockMemoryServer server)
    {
        servers.Remove($"{server.Host}:{server.Port}");
    }

    internal class MockMemoryDatabase
    {
        private readonly MockMemoryServer server;
        private readonly string databaseName;
        private readonly Dictionary<string, Table> tables = new(StringComparer.InvariantCultureIgnoreCase);
        
        public string Name => databaseName;
            
        public MockMemoryDatabase(MockMemoryServer server, string databaseName)
        {
            this.server = server;
            this.databaseName = databaseName;
        }

        public class Table
        {
            public TableType TableType { get; }
            public IReadOnlyList<ColumnInfo> ColumnInfos => columnInfo;

            public readonly string Name;
            private readonly Type[] types;
            private readonly ColumnInfo[] columnInfo;
            private List<object?[]> rows = new();

            public Table(string name, TableType tableType, ColumnInfo[] columnInfo)
            {
                TableType = tableType;
                Name = name;
                this.types = columnInfo.Select(x => MySqlType.TryParse(x.Type, out var type) ? type.ManagedType : throw new Exception("Unable to parse type " + x.Type)).ToArray();
                this.columnInfo = columnInfo;
            }

            public int DeleteWhere(Func<object?[], bool> where)
            {
                return rows.RemoveIf(where);
            }

            public int DeleteWhereColumnEquals(string[] columnNames, object?[] values)
            {
                var columnIndices = columnNames.Select(GetColumnIndexByName).ToArray();
                return DeleteWhere(row => columnIndices.Select((columnIndex, i) => (columnIndex, i))
                    .All(pair => row[pair.columnIndex] == values[pair.i]));
            }
            
            public void Insert(params object?[] columnValues)
            {
                if (columnValues.Length != columnInfo.Length)
                    throw new ArgumentOutOfRangeException($"[{Name}] Inserted row must have same length as columnInfo ({columnValues.Length} != {columnInfo.Length})");
                rows.Add(columnValues);
            }
            
            public SelectResult SelectColumns(string[] columns, Func<Dictionary<string, object?>, bool>? predicate)
            {
                string ExtractColumnName(string columnName)
                {
                    if (columnName.Contains(" AS "))
                    {
                        var parts = columnName.Split(" AS ");
                        if (parts.Length != 2)
                            throw MySqlExceptions.Create(MySqlErrorCode.WrongColumnName, $"Unknown column '{columnName}'");
                        return parts[0].Trim();
                    }
                    return columnName;
                }
                string ExtractColumnAlias(string columnName)
                {
                    if (columnName.Contains(" AS "))
                    {
                        var parts = columnName.Split(" AS ");
                        if (parts.Length != 2)
                            throw MySqlExceptions.Create(MySqlErrorCode.WrongColumnName, $"Unknown column '{columnName}'");
                        return parts[1].Trim();
                    }
                    return columnName;
                }

                var expandedColumns = columns.ToList();
                for (int i = expandedColumns.Count - 1; i >= 0; --i)
                {
                    if (expandedColumns[i] == "*")
                    {
                        expandedColumns.RemoveAt(i);
                        expandedColumns.InsertRange(i, columnInfo.Select(x => x.Name));
                    }
                }
                
                var selectColumnIndices = expandedColumns.Select(ExtractColumnName).Select(GetColumnIndexByName).ToArray();
                string[] columnNames = expandedColumns.Select(ExtractColumnAlias).ToArray();
                Type?[] columnTypes = selectColumnIndices.Select(i => types[i]).ToArray();
                string?[] columnSqlTypes = selectColumnIndices.Select(i => columnInfo[i].Type).ToArray();
                List<int> selectRowIndices = new();
                for (int i = 0; i < rows.Count; ++i)
                {
                    if (predicate != null)
                    {
                        var rowAsDict = rows[i].Select((val, index) => (val, columnInfo[index].Name))
                            .ToDictionary(x => x.Name, x => x.val);
                        if (!predicate(rowAsDict))
                            continue;
                    }
                    selectRowIndices.Add(i);
                }

                object?[,] data = new object[selectRowIndices.Count, selectColumnIndices.Length];
                for (int i = 0; i < selectRowIndices.Count; ++i)
                {
                    for (int j = 0; j < selectColumnIndices.Length; ++j)
                    {
                        data[i, j] = rows[selectRowIndices[i]][selectColumnIndices[j]];
                    }
                }
                
                return SelectResult.Query(columnNames, columnTypes, selectRowIndices.Count, ConvertData(columnSqlTypes, columnTypes, data));
            }

            private int GetColumnIndexByName(string columnName)
            {
                for (int i = 0; i < columnInfo.Length; ++i)
                    if (columnInfo[i].Name == columnName)
                        return i;
                
                throw MySqlExceptions.Create(MySqlErrorCode.WrongColumnName, $"Unknown column '{columnName}'");
            }
            
            private class MockMySqlDataReader : IMySqlDataReader
            {
                private object?[,] data;
                private int rowIndex;

                public MockMySqlDataReader(object?[,] data, int rowIndex)
                {
                    this.data = data;
                    this.rowIndex = rowIndex;
                }

                public bool IsDBNull(int ordinal) => data[rowIndex, ordinal] == null;
                public object? GetValue(int ordinal) => data[rowIndex, ordinal];
                public string? GetString(int ordinal) => data[rowIndex, ordinal] as string;
                public bool GetBoolean(int ordinal) => (bool?)data[rowIndex, ordinal] ?? false;
                public byte GetByte(int ordinal) => (byte?)data[rowIndex, ordinal] ?? 0;
                public sbyte GetSByte(int ordinal) => (sbyte?)data[rowIndex, ordinal] ?? 0;
                public short GetInt16(int ordinal) => (short?)data[rowIndex, ordinal] ?? 0;
                public ushort GetUInt16(int ordinal) => (ushort?)data[rowIndex, ordinal] ?? 0;
                public int GetInt32(int ordinal) => (int?)data[rowIndex, ordinal] ?? 0;
                public uint GetUInt32(int ordinal) => (uint?)data[rowIndex, ordinal] ?? 0;
                public long GetInt64(int ordinal) => (long?)data[rowIndex, ordinal] ?? 0;
                public ulong GetUInt64(int ordinal) => (ulong?)data[rowIndex, ordinal] ?? 0;
                public char GetChar(int ordinal) => (char?)data[rowIndex, ordinal] ?? '\0';
                public PublicMySqlDecimal GetDecimal(int ordinal) => (PublicMySqlDecimal?)data[rowIndex, ordinal] ?? PublicMySqlDecimal.Zero;
                public double GetDouble(int ordinal) => (double?)data[rowIndex, ordinal] ?? 0;
                public float GetFloat(int ordinal) => (float?)data[rowIndex, ordinal] ?? 0;
                public DateTime GetDateTime(int ordinal) => (DateTime?)data[rowIndex, ordinal] ?? DateTime.MinValue;
                public DateTimeOffset GetDateTimeOffset(int ordinal) => (DateTimeOffset?)data[rowIndex, ordinal] ?? DateTimeOffset.MinValue;
                public Guid GetGuid(int ordinal) => (Guid?)data[rowIndex, ordinal] ?? Guid.Empty;
                public TimeSpan GetTimeSpan(int ordinal) => (TimeSpan?)data[rowIndex, ordinal] ?? TimeSpan.Zero;
                public long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
                {
                    var bytes = (byte[])data[rowIndex, ordinal]!;
                    if (buffer == null)
                        return bytes.Length;
                    Array.Copy(bytes, dataOffset, buffer, bufferOffset, length);
                    return bytes.Length;
                }
                public MySqlDateTime GetMySqlDateTime(int ordinal) => (MySqlDateTime?)data[rowIndex, ordinal] ?? new MySqlDateTime();
            }
            
            private IColumnData[] ConvertData(string?[] sqlTypes, Type?[] types, object?[,] data)
            {
                IColumnData[] columns = new IColumnData[types.Length];
                MockMySqlDataReader[] rows = new MockMySqlDataReader[data.GetLength(0)];
                for (int i = 0; i < rows.Length; ++i)
                    rows[i] = new MockMySqlDataReader(data, i);
                
                for (int i = 0; i < columns.Length; ++i)
                {
                    columns[i] = IColumnData.CreateTypedColumn(types[i], sqlTypes[i]);
                    for (int j = 0; j < data.GetLength(0); ++j)
                        columns[i].Append(rows[j], i);
                }

                return columns;
            }
        }
        
        internal Table CreateTableInternal(string tableName, TableType tableType, params ColumnInfo[] columns)
        {
            var table = tables[tableName] = new Table(tableName, tableType, columns);
            return table;
        }

        internal void FinalizeCreateTableInternal(Table table)
        {
            server.TablesTable.Insert(new object?[]
            {
                "def",
                databaseName,
                table.Name,
                table.TableType == TableType.Table ? "BASE TABLE" : "VIEW",
                "MEMORY",
                10,
                "FIXED",
                0ul,
                0ul,
                0ul,
                0ul,
                0ul,
                0ul,
                null,
                new MySqlDateTime(DateTime.Now),
                null,
                null,
                "utf8mb4_general_ci",
                null,
                null,
                null
            });
            for (var index = 0; index < table.ColumnInfos.Count; index++)
            {
                var column = table.ColumnInfos[index];
                server.ColumnsTable.Insert(
                    "def",
                    databaseName,
                    table.Name,
                    column.Name,
                    (uint)(index + 1),
                    column.DefaultValue,
                    column.IsNullable ? "YES" : "NO",
                    column.Type,
                    (long)0,
                    (long)0,
                    (ulong?)null,
                    (ulong?)null,
                    (uint?)null,
                    "utf8mb4",
                    "utf8mb4_general_ci",
                    column.Type,
                    column.IsPrimaryKey ? "PRI" : "",
                    column.IsAutoIncrement ? "auto_increment" : "",
                    "select",
                    "comment here",
                    null,
                    (uint?)null);
            }
        }
        
        public Table CreateTable(string tableName, TableType tableType, params ColumnInfo[] columns)
        {
            var table = CreateTableInternal(tableName, tableType, columns);
            FinalizeCreateTableInternal(table);
            var createInfo = new StringBuilder();
            createInfo.Append("CREATE ");
            if (tableType == TableType.View)
                createInfo.Append("VIEW ");
            else
                createInfo.Append("TABLE ");
            createInfo.AppendLine($"`{tableName}` (");

            List<string> toCreate = new();
            foreach (var col in columns)
            {
                var columnInfo = $"`{col.Name}` {col.Type}";
                if (col.IsAutoIncrement)
                    columnInfo += " AUTO_INCREMENT";
                if (!col.IsNullable)
                    columnInfo += " NOT NULL";
                if (col.DefaultValue != null)
                    columnInfo += $" DEFAULT '{col.DefaultValue}'";
                toCreate.Add(columnInfo);
            }
            if (columns.Any(x => x.IsPrimaryKey))
                toCreate.Add($"PRIMARY KEY ({string.Join(", ", columns.Where(x => x.IsPrimaryKey).Select(x => $"`{x.Name}`"))})");

            createInfo.AppendLine(string.Join(",\n", toCreate.Select(x => $"    {x}")));
            createInfo.AppendLine(");");
            // Console.WriteLine(createInfo.ToString());
            
            return table;
        }

        public IReadOnlyList<(string name, TableType type)> GetTables()
        {
            return tables.Select(t => (t.Key, t.Value.TableType)).ToList();
        }

        public Table GetTable(string tableName)
        {
            if (!tables.TryGetValue(tableName, out var table))
                throw MySqlExceptions.Create(MySqlErrorCode.UnknownTable, $"Unknown table '{tableName}'");
            return table;
        }

        public void Drop(Table table)
        {
            tables.Remove(table.Name);
            Debug.Assert(server.TablesTable.DeleteWhereColumnEquals(new []{"TABLE_SCHEMA", "TABLE_NAME"}, new object?[]{Name, table.Name}) == 1);
            Debug.Assert(server.ColumnsTable.DeleteWhereColumnEquals(new []{"TABLE_SCHEMA", "TABLE_NAME"}, new object?[]{Name, table.Name}) == table.ColumnInfos.Count);
        }
    }
}