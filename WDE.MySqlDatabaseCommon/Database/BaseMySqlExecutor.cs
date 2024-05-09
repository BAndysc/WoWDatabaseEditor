using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MySqlConnector;
using Prism.Events;
using WDE.Common.Database;
using WDE.Common.Events;
using WDE.Common.Tasks;
using WDE.MySqlDatabaseCommon.Services;
using WDE.SqlInterpreter;
using WDE.SqlQueryGenerator;

namespace WDE.MySqlDatabaseCommon.Database
{
    public abstract class BaseMySqlExecutor : IMySqlExecutor, IMySqlHotfixExecutor
    {
        private readonly string connectionString;
        private readonly string databaseName;
        private readonly IDatabaseProvider databaseProvider;
        private readonly IQueryEvaluator queryEvaluator;
        private readonly IEventAggregator eventAggregator;
        private readonly IMainThread mainThread;
        private readonly DatabaseLogger databaseLogger;
        
        public abstract bool IsConnected { get; }

        public BaseMySqlExecutor(string connectionString, 
            string databaseName,
            IDatabaseProvider databaseProvider,
            IQueryEvaluator queryEvaluator,
            IEventAggregator eventAggregator,
            IMainThread mainThread,
            DatabaseLogger databaseLogger)
        {
            this.connectionString = connectionString;
            this.databaseName = databaseName;
            this.databaseProvider = databaseProvider;
            this.queryEvaluator = queryEvaluator;
            this.eventAggregator = eventAggregator;
            this.mainThread = mainThread;
            this.databaseLogger = databaseLogger;
        }

        public async Task<IList<string>> GetTables()
        {
            var tables = await ExecuteSelectSql("SHOW TABLES");

            return tables.Select(row => tables.Value<string>(row, 0)!).ToList();
        }

        public async Task<IList<MySqlDatabaseColumn>> GetTableColumns(string table)
        {
            var columns = await ExecuteSelectSql($"SELECT COLUMN_NAME, COLUMN_DEFAULT, IS_NULLABLE, DATA_TYPE, COLUMN_TYPE, COLUMN_KEY FROM information_schema.columns WHERE table_name = '{table}' AND table_schema = '{databaseName}' ORDER BY ordinal_position;");

            var columnNameIndex = columns.ColumnIndex("COLUMN_NAME");
            var dataTypeIndex = columns.ColumnIndex("DATA_TYPE");
            var columnTypeIndex = columns.ColumnIndex("COLUMN_TYPE");
            var columnDefaultIndex = columns.ColumnIndex("COLUMN_DEFAULT");
            var isNullableIndex = columns.ColumnIndex("IS_NULLABLE");
            var columnKeyIndex = columns.ColumnIndex("COLUMN_KEY");

            var list = new List<MySqlDatabaseColumn>();
            foreach (var rowIndex in columns)
            {
                var c = new MySqlDatabaseColumn()
                {
                    ColumnName = columns.Value<string>(rowIndex, columnNameIndex)!
                };

                c.Nullable = columns.Value<string>(rowIndex, isNullableIndex)! == "YES";
                c.PrimaryKey = columns.Value<string>(rowIndex, columnKeyIndex)! == "PRI";

                bool unsigned = (columns.Value<string>(rowIndex, columnTypeIndex)!).Contains("unsigned");
                string dataType = columns.Value<string>(rowIndex, dataTypeIndex)!;
                string? defaultValue = null;
                if (columns.Value<string>(rowIndex, columnDefaultIndex) is string str)
                    defaultValue = str;

                c.DatabaseType = dataType;
                c.ManagedType = null;
                switch (dataType)
                {
                    case "float":
                        c.ManagedType = typeof(float);
                        if (defaultValue != null && float.TryParse(defaultValue, out var defaultFloat))
                            c.DefaultValue = defaultFloat;
                        break;
                    case "int":
                        c.ManagedType = unsigned ? typeof(ulong) : typeof(long);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(ulong))
                            {
                                if (ulong.TryParse(defaultValue, out var defaultInt))
                                    c.DefaultValue = defaultInt;
                            }
                            else
                            {
                                if (long.TryParse(defaultValue, out var defaultInt))
                                    c.DefaultValue = defaultInt;
                            }
                        break;
                    case "tinyint":
                        c.ManagedType = unsigned ? typeof(byte) : typeof(sbyte);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(byte))
                            {
                                if (byte.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                            else
                            {
                                if (sbyte.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                        break;
                    case "smallint":
                        c.ManagedType = unsigned ? typeof(ushort) : typeof(short);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(ushort))
                            {
                                if (ushort.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                            else
                            {
                                if (short.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                        break;
                    case "mediumint":
                        c.ManagedType = unsigned ? typeof(uint) : typeof(int);
                        if (defaultValue != null)
                            if (c.ManagedType == typeof(uint))
                            {
                                if (uint.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                            else
                            {
                                if (int.TryParse(defaultValue, out var defaultV))
                                    c.DefaultValue = defaultV;
                            }
                        break;
                    case "char":
                    case "varchar":
                    case "text":
                    case "mediumtext":
                    case "longtext":
                        c.ManagedType = typeof(string);
                        c.DefaultValue = defaultValue;
                        break;
                }
                list.Add(c);
            }
            return list;
        }

        public Task ExecuteSql(IQuery query, bool rollback)
        {
            return ExecuteSql(query.QueryString, rollback);
        }
        
        public async Task ExecuteSql(string query, bool rollback)
        {
            if (string.IsNullOrWhiteSpace(query) || !IsConnected)
                return;

            databaseLogger.Log(query, null, TraceLevel.Info, QueryType.WriteQuery);
            
            using var writeLock = await DatabaseLock.WriteLock();
            
            MySqlConnection conn = new(connectionString);
            MySqlTransaction transaction;
            try
            {
                await conn.OpenAsync();
                transaction = await conn.BeginTransactionAsync();
            }
            catch (Exception e)
            {
                throw new IMySqlExecutor.CannotConnectToDatabaseException(e);
            }

            try
            {
                MySqlCommand cmd = new(query, conn, transaction);
                await cmd.ExecuteNonQueryAsync();
                if (rollback)
                    await transaction.RollbackAsync();
                else
                    await transaction.CommitAsync();
            }
            catch (MySqlConnector.MySqlException e)
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(e.Message, e);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }
            finally
            {
                await conn.CloseAsync();
            }

            foreach (var tableName in queryEvaluator.Extract(query).Select(q => q.TableName).Distinct())
            {
                mainThread.Dispatch(() =>
                    eventAggregator.GetEvent<DatabaseTableChanged>().Publish(tableName));
            }
        }

        private class DatabaseSelectResult : IDatabaseSelectResult
        {
            private readonly string[] columns;
            private readonly Type[] types;
            private readonly List<object?[]> rows;

            public DatabaseSelectResult(string[] columns, Type[] types, List<object?[]> rows)
            {
                this.columns = columns;
                this.types = types;
                this.rows = rows;
            }

            public int Columns => columns.Length;

            public int Rows => rows.Count;

            public string ColumnName(int index) => columns[index];

            public Type ColumnType(int index) => types[index];

            public object? Value(int row, int column) => rows[row][column];

            public T? Value<T>(int row, int column)
            {
                if (rows[row][column] == null)
                    return default;
                return (T) rows[row][column]!;
            }

            public bool IsNull(int row, int column) => rows[row][column] == null;

            public int ColumnIndex(string columnName)
            {
                for (int i = 0; i < columns.Length; ++i)
                    if (columns[i] == columnName)
                        return i;

                throw new Exception($"Column {columnName} not found. Have columns: " + string.Join(", ", columns));
            }

            public IEnumerator<int> GetEnumerator()
            {
                for (int i = 0; i < Rows; ++i)
                    yield return i;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public async Task<IDatabaseSelectResult> ExecuteSelectSql(string query)
        {
            if (string.IsNullOrEmpty(query) || !IsConnected)
                return new EmptyDatabaseSelectResult();

            databaseLogger.Log(query, null, TraceLevel.Info, QueryType.ReadQuery);
            using var writeLock = await DatabaseLock.WriteLock();
            
            MySqlConnection conn = new(connectionString);
            try
            {
                await conn.OpenAsync();
            }
            catch (Exception e)
            {
                throw new IMySqlExecutor.CannotConnectToDatabaseException(e);
            }

            MySqlDataReader reader;
            try
            {
                MySqlCommand cmd = new(query, conn);
                reader = await cmd.ExecuteReaderAsync();
            }
            catch (MySqlConnector.MySqlException e)
            {
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(e.Message, e);
            }
            catch (Exception ex)
            {
                await conn.CloseAsync();
                throw new IMySqlExecutor.QueryFailedDatabaseException(ex);
            }

            string[] columns = new string[reader.FieldCount];
            Type[] types = new Type[reader.FieldCount];
            List<object?[]> rows = new List<object?[]>();
            for (int i = 0; i < reader.FieldCount; ++i)
            {
                columns[i] = reader.GetName(i);
                types[i] = reader.GetFieldType(i);
            }
            while (reader.Read())
            {
                object?[] row = new object?[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; ++i)
                {
                    row[i] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            await reader.CloseAsync();
            await conn.CloseAsync();
            return new DatabaseSelectResult(columns, types, rows);
        }
    }
}