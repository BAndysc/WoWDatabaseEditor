using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    public static class Queries
    {
        public static IMultiQuery BeginTransaction(DataDatabaseType type)
        {
            return new MultiQuery(type);
        }

        public static ITable Table(DatabaseTable name)
        {
            return new Table(name);
        }

        public static IQuery Empty(DataDatabaseType type)
        {
            return new Query(type, "");
        }

        public static IQuery Raw(DataDatabaseType type, string sql)
        {
            return new Query(type, sql);
        }
    }
}