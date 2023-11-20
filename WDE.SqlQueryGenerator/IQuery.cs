using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    public interface IQuery
    {
        string QueryString { get; }
        DataDatabaseType Database { get; }
    }
}