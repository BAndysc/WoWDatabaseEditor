using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Module.Attributes;
using WDE.SqlQueryGenerator;

namespace WDE.Common.Database
{
    public interface IDatabaseSelectResult : IEnumerable<int>
    {
        int Columns { get; }
        int Rows { get; }
        string ColumnName(int index);
        Type ColumnType(int index);
        object? Value(int row, int column);
        T? Value<T>(int row, int column);
        bool IsNull(int row, int column);
        int ColumnIndex(string columnName);
    }

    [UniqueProvider]
    public interface IMySqlExecutor
    {
        bool IsConnected { get; }
        
        Task ExecuteSql(IQuery query, bool rollback = false);
        Task ExecuteSql(string query, bool rollback = false);
        Task<IDatabaseSelectResult> ExecuteSelectSql(string query);
        Task<IList<string>> GetTables();
        Task<IList<MySqlDatabaseColumn>> GetTableColumns(string table);

        public class DatabaseExecutorException : Exception
        {
            protected DatabaseExecutorException(string message, Exception inner) : base(message, inner)
            {
            }
        }

        public class CannotConnectToDatabaseException : DatabaseExecutorException
        {
            public CannotConnectToDatabaseException(Exception inner) : base("Cannot connect to database", inner)
            {
            }
        }

        public class QueryFailedDatabaseException : DatabaseExecutorException
        {
            public QueryFailedDatabaseException(string message, Exception inner) : base(message, inner)
            {
            }
            public QueryFailedDatabaseException(Exception inner) : base("Failed to execute query", inner)
            {
            }
        }
    }

    [UniqueProvider]
    public interface IMySqlHotfixExecutor
    {
        bool IsConnected { get; }

        Task ExecuteSql(IQuery query, bool rollback = false);
        Task ExecuteSql(string query, bool rollback = false);
        Task<IDatabaseSelectResult> ExecuteSelectSql(string query);
        Task<IList<string>> GetTables();
        Task<IList<MySqlDatabaseColumn>> GetTableColumns(string table);
    }

    public struct MySqlDatabaseColumn
    {
        public string ColumnName { get; set; }
        public bool Nullable { get; set; }
        public bool PrimaryKey { get; set; }
        public string DatabaseType { get; set; }
        public Type? ManagedType { get; set; }
        public object? DefaultValue { get; set; }
        public override string ToString() => ColumnName;
    }

    public class EmptyDatabaseSelectResult : IDatabaseSelectResult
    {
        public static EmptyDatabaseSelectResult Instance { get; } = new();

        public int Columns => 0;
        public int Rows => 0;
        public string ColumnName(int index) => throw new ArgumentOutOfRangeException();
        public Type ColumnType(int index) => throw new ArgumentOutOfRangeException();
        public object? Value(int row, int column) => throw new ArgumentOutOfRangeException();
        public T? Value<T>(int row, int column) => throw new ArgumentOutOfRangeException();
        public bool IsNull(int row, int column) => throw new ArgumentOutOfRangeException();
        public int ColumnIndex(string columnName) => throw new ArgumentOutOfRangeException();

        public IEnumerator<int> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}