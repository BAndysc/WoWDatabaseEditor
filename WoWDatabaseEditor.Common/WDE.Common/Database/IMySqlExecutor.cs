using System;
using System.Threading.Tasks;
using WDE.Module.Attributes;

namespace WDE.Common.Database
{
    [UniqueProvider]
    public interface IMySqlExecutor
    {
        Task ExecuteSql(string query);

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
}