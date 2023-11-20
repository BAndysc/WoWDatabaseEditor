using WDE.Common.Database;

namespace WDE.SqlQueryGenerator
{
    public interface IMultiQuery
    {
        DataDatabaseType Database { get; }
        void Add(IQuery? query);
        IQuery Close();
        ITable Table(DatabaseTable name);
    }
}