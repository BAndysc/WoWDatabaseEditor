using System;
using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    internal class BlankQuery : IQuery
    {
        public BlankQuery(IMultiQuery multiQuery)
        {
            multiQuery.Add(this);
            Database = multiQuery.Database;
        }

        public string QueryString => Environment.NewLine;
        public DataDatabaseType Database { get; }
        public override string ToString() => QueryString;
    }
    
    internal class Query : IQuery
    {
        public Query(DataDatabaseType database, string query)
        {
            Database = database;
            QueryString = query;
        }

        public Query(ITable table, string query)
        {
            Database = table.TableName.Database;
            table?.CurrentQuery?.Add(this);
            QueryString = query;
        }
        
        internal Query(IMultiQuery multiQuery, string query)
        {
            Database = multiQuery.Database;
            multiQuery.Add(this);
            QueryString = query;
        }

        public string QueryString { get; }

        public DataDatabaseType Database { get; }

        public override string ToString() => QueryString;
    }
}