using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WDE.Common.Annotations;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IMySqlExecutor
    {
        bool IsConnected { get; }
        
        Task ExecuteSql(string query);
        Task<IList<Dictionary<string, (Type, object)>>> ExecuteSelectSql(string query);
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
            public QueryFailedDatabaseException(Exception inner) : base("Failed to execute query", inner)
            {
            }
        }
    }

    public struct MySqlDatabaseColumn
    {
        public string ColumnName { get; set; }
        public bool Nullable { get; set; }
        public bool PrimaryKey { get; set; }
        public string DatabaseType { get; set; }
        public Type? ManagedType { get; set; }
        public object DefaultValue { get; set; }
    }
}